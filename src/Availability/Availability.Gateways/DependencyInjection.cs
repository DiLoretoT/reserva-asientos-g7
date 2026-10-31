using Availability.Gateways.Persistencia;
using Availability.Gateways.Persistencia.Repositorios;
using Availability.Gateways.Booking;
using Availability.UseCases.Interfaces;
using Availability.UseCases.Eventos;
using Availability.UseCases.Solicitudes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

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
            OpcionesBooking opcionesBooking)
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

            // Cliente HTTP de Booking. AddHttpClient lo registra con un HttpClient que sale de
            // IHttpClientFactory, la fábrica de .NET que reutiliza las conexiones en lugar de
            // abrir una nueva en cada pedido. Cada llamada pasa por TokenServicioHandler, que
            // agrega el token.
            services.AddHttpClient<ClienteBooking>(http =>
                {
                    http.BaseAddress = new Uri(opcionesBooking.BaseUrl);
                    http.Timeout = TiempoDeEspera;
                })
                .AddHttpMessageHandler<TokenServicioHandler>();

            // Los dos puertos hacia Booking se resuelven con la misma clase. Quien pide
            // IConsultaOcupacion o IConsultaEstadoSolicitud recibe un ClienteBooking.
            services.AddTransient<IConsultaOcupacion>(sp => sp.GetRequiredService<ClienteBooking>());
            services.AddTransient<IConsultaEstadoSolicitud>(sp => sp.GetRequiredService<ClienteBooking>());

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