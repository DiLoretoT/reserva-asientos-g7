using Availability.Gateways.Persistencia;
using Availability.Gateways.Persistencia.Repositorios;
using Availability.Gateways.Booking;
using Availability.Gateways.Broker;
using Availability.UseCases.Interfaces;
using Microsoft.Extensions.Logging;
using Availability.UseCases.Eventos;
using Availability.UseCases.Solicitudes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Availability.Gateways
{
    // Registro de los adaptadores de Gateways en la inyección de dependencias.
    //
    // Cada proyecto expone su propio método de extensión, así Program.cs solo agrega una
    // línea por proyecto y los integrantes no se pisan editando el mismo archivo. Además,
    // Program.cs no necesita conocer las clases concretas (EventoRepository, el
    // DbContext): solo sabe que existe AddGateways.
    public static class DependencyInjection
    {
        public static IServiceCollection AddGateways(
            this IServiceCollection services,
            string connectionString,
            bool simularBookingSinRespuesta,
            bool simularBrokerSinRespuesta)
        {
            services.AddDbContext<AvailabilityDbContext>(options =>
                options.UseSqlite(connectionString));

            // Scoped: una instancia por pedido HTTP, igual que el DbContext que usan.
            // Cuando un caso de uso pida IEventoRepository, recibe un EventoRepository.
            services.AddScoped<IEventoRepository, EventoRepository>();
            services.AddScoped<ISolicitudRepository, SolicitudRepository>();

            // Mientras Booking no tenga su API, IConsultaOcupacion se resuelve con la
            // simulación. Cuando exista, esta línea se reemplaza por el registro del cliente
            // HTTP y el resto del servicio no cambia.
            // Es Singleton, una sola instancia para toda la aplicación, porque no guarda datos
            // de ningún pedido. La opción de simular la falla llega desde Program.cs, igual
            // que el connection string, para que Gateways no lea la configuración por su cuenta.
            services.AddSingleton<IConsultaOcupacion>(sp => new ConsultaOcupacionSimulada(
                simularBookingSinRespuesta,
                sp.GetRequiredService<ILogger<ConsultaOcupacionSimulada>>()));

            // Mientras no tengamos el connection string del broker, IPublicadorSolicitudes se
            // resuelve con la simulación. Cuando lo tengamos, esta línea se reemplaza por el
            // registro del publicador de Azure Service Bus y el caso de uso no cambia.
            // Singleton por el mismo motivo que la consulta a Booking: no guarda datos de
            // ningún pedido.
            services.AddSingleton<IPublicadorSolicitudes>(sp => new PublicadorSolicitudesSimulado(
                simularBrokerSinRespuesta,
                sp.GetRequiredService<ILogger<PublicadorSolicitudesSimulado>>()));

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
