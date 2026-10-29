using Availability.Gateways.Persistencia;
using Availability.Gateways.Persistencia.Repositorios;
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
        public static IServiceCollection AddGateways(this IServiceCollection services, string connectionString)
        {
            services.AddDbContext<AvailabilityDbContext>(options =>
                options.UseSqlite(connectionString));

            // Scoped: una instancia por pedido HTTP, igual que el DbContext que usan.
            // Cuando un caso de uso pida IEventoRepository, recibe un EventoRepository.
            services.AddScoped<IEventoRepository, EventoRepository>();
            services.AddScoped<ISolicitudRepository, SolicitudRepository>();

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
