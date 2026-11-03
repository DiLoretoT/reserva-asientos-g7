using Availability.Gateways.Persistencia;
using Availability.Gateways.Persistencia.Repositorios;
using Availability.Gateways.Booking;
using Availability.Gateways.Broker;
using Availability.Gateways.Observabilidad;
using Availability.UseCases.Interfaces;
using Availability.UseCases.Eventos;
using Availability.UseCases.Solicitudes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Azure.Messaging.ServiceBus;
using Shared.Observability;

namespace Availability.Gateways
{
    // Registro de los adaptadores de Gateways en la inyección de dependencias.
    // Cada proyecto expone su propio método de extensión, así Program.cs solo agrega una
    // línea por proyecto y los integrantes no se pisan editando el mismo archivo. Además,
    // Program.cs no necesita conocer las clases concretas (EventoRepository, el
    // DbContext). Solo sabe que existe AddGateways.
    public static class DependencyInjection
    {
        // Tiempo máximo de espera de cada llamada a Booking y a Keycloak. HttpClient espera
        // 100 segundos por defecto; con un servicio caído, la página quedaría colgada ese
        // tiempo antes de mostrar el aviso.
        private static readonly TimeSpan TiempoDeEspera = TimeSpan.FromSeconds(5);

        public static IServiceCollection AddGateways(
            this IServiceCollection services,
            string connectionString,
            OpcionesBooking opcionesBooking,
            bool simularBrokerSinRespuesta,
            string? serviceBusConnectionString,
            string? serviceBusTopicName)
        {
            services.AddDbContext<AvailabilityDbContext>(options =>
                options.UseSqlite(connectionString));

            // Scoped: una instancia por pedido HTTP, igual que el DbContext que usan.
            // Cuando un caso de uso pida IEventoRepository, recibe un EventoRepository.
            services.AddScoped<IEventoRepository, EventoRepository>();
            services.AddScoped<ISolicitudRepository, SolicitudRepository>();

            // Token de servicio de availability-web. TokenServicio es Singleton para que el
            // token guardado en memoria se comparta entre pedidos. El handler es Transient
            // porque la fábrica de HttpClient arma uno nuevo cada vez que renueva la cadena
            // de handlers.
            services.AddSingleton(opcionesBooking);
            services.AddHttpClient(TokenServicio.NombreCliente, http => http.Timeout = TiempoDeEspera);
            services.AddSingleton<TokenServicio>();
            services.AddTransient<TokenServicioHandler>();

            // Handler de Shared.Observability que agrega el header X-Trace-Id a cada llamada
            // (contracts/logs.md). Transient por el mismo motivo que TokenServicioHandler.
            services.AddTransient<TraceIdHttpClientHandler>();

            // Cliente HTTP de Booking. AddHttpClient lo registra con un HttpClient que sale de
            // IHttpClientFactory, la fábrica de .NET que reutiliza las conexiones en lugar de
            // abrir una nueva en cada pedido. Cada llamada pasa por TokenServicioHandler, que
            // agrega el token, y por TraceIdHttpClientHandler, que agrega el TraceId: así
            // Booking registra sus logs con el mismo TraceId que Availability y la cadena de
            // sucesos se puede seguir entre los dos servicios.
            //
            // El cliente de Keycloak (TokenServicio.NombreCliente) no lleva el TraceId: Keycloak
            // no participa del contrato de logs.
            services.AddHttpClient<ClienteBooking>(http =>
                {
                    http.BaseAddress = new Uri(opcionesBooking.BaseUrl);
                    http.Timeout = TiempoDeEspera;
                })
                .AddHttpMessageHandler<TokenServicioHandler>()
                .AddHttpMessageHandler<TraceIdHttpClientHandler>();

            // Los dos puertos hacia Booking se resuelven con la misma clase. Quien pide
            // IConsultaOcupacion o IConsultaEstadoSolicitud recibe un ClienteBooking.
            services.AddTransient<IConsultaOcupacion>(sp => sp.GetRequiredService<ClienteBooking>());
            services.AddTransient<IConsultaEstadoSolicitud>(sp => sp.GetRequiredService<ClienteBooking>());

            // Publicador de la solicitud: si la configuración trae la connection string
            // del broker (ServiceBus:ConnectionString), se usa el publicador real de
            // Azure Service Bus, con su ServiceBusClient como Singleton, porque el
            // cliente es thread-safe y mantiene la conexión AMQP que es lo caro de armar.
            // Si la clave no está configurada, queda la simulación, para que los entornos
            // de los compañeros que no cargaron secretos sigan arrancando igual. El
            // publicador se anota Singleton porque no guarda datos de ningún pedido.
            if (string.IsNullOrWhiteSpace(serviceBusConnectionString))
            {
                services.AddSingleton<IPublicadorSolicitudes>(sp => new PublicadorSolicitudesSimulado(
                    simularBrokerSinRespuesta,
                    sp.GetRequiredService<ILogger<PublicadorSolicitudesSimulado>>()));
            }
            else
            {
                services.AddSingleton(_ => new ServiceBusClient(serviceBusConnectionString));
                services.AddSingleton<IPublicadorSolicitudes>(sp => new PublicadorSolicitudesServiceBus(
                    sp.GetRequiredService<ServiceBusClient>(),
                    serviceBusTopicName,
                    sp.GetRequiredService<ILogger<PublicadorSolicitudesServiceBus>>()));
            }

            // Auditoría de los casos de uso (issue #9). Singleton porque no guarda estado propio:
            // IAuditLogger e ITraceContext ya son Singleton (los registra AddObservability en
            // Program.cs), y el TraceId de cada pedido vive en el AsyncLocal de ITraceContext,
            // no en esta clase.
            services.AddSingleton<IAuditoria, AuditoriaObservability>();

            return services;
        }

        // Crea la base y aplica las migraciones pendientes al arrancar el sitio. Así nadie
        // del grupo tiene que acordarse de ejecutar "dotnet ef database update": alcanza
        // con ejecutar Availability. Program.cs lo llama solo en Development.
        public static void AplicarMigraciones(this IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AvailabilityDbContext>();
            db.Database.Migrate();
        }
    }
}