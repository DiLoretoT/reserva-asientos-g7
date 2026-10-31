using Availability.Gateways.Persistencia.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Availability.Gateways.Persistencia
{
    // Contexto de Entity Framework de Availability: representa la base propia del servicio.
    //
    // Cada microservicio tiene su propia base (patrón Database per Service, como muestra el
    // diagrama). Esta base guarda solo lo que es de Availability: qué eventos y asientos
    // existen y qué solicitudes se registraron. Las reservas y su estado están en la base
    // de Booking, y Availability nunca la lee directamente: se los pide por REST.
    //
    // El motor es SQLite y no SQL Server, aunque el diagrama dice "SQL Database". SQLite
    // también es una base SQL, guarda todo en un solo archivo y no requiere instalar un
    // servidor, así que funciona igual en la PC de cada integrante. Como el acceso pasa por
    // Entity Framework, cambiar a SQL Server es reemplazar UseSqlite por UseSqlServer en
    // DependencyInjection.cs y el connection string; ni este contexto ni los casos de uso
    // cambian.
    public class AvailabilityDbContext : DbContext
    {
        public AvailabilityDbContext(DbContextOptions<AvailabilityDbContext> options)
            : base(options)
        {
        }

        public DbSet<EventoDb> Eventos => Set<EventoDb>();

        public DbSet<AsientoDb> Asientos => Set<AsientoDb>();

        public DbSet<SolicitudReservaDb> Solicitudes => Set<SolicitudReservaDb>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Un evento no puede tener dos veces el mismo asiento (por ejemplo, dos B-12).
            // La base lo garantiza con un índice único, aunque el código tenga un error.
            modelBuilder.Entity<AsientoDb>()
                .HasIndex(a => new { a.EventoId, a.Fila, a.Numero })
                .IsUnique();

            // Si se borra un evento, se borran sus asientos: un asiento no existe sin evento.
            modelBuilder.Entity<AsientoDb>()
                .HasOne(a => a.Evento)
                .WithMany(e => e.Asientos)
                .HasForeignKey(a => a.EventoId)
                .OnDelete(DeleteBehavior.Cascade);

            // Las solicitudes, en cambio, no se borran en cascada: son el registro de lo que
            // pidió cada usuario y Booking puede tener reservas asociadas a ellas. La base
            // impide borrar un evento o un asiento que ya tiene solicitudes.
            modelBuilder.Entity<SolicitudReservaDb>()
                .HasOne(s => s.Evento)
                .WithMany()
                .HasForeignKey(s => s.EventoId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SolicitudReservaDb>()
                .HasOne(s => s.Asiento)
                .WithMany()
                .HasForeignKey(s => s.AsientoId)
                .OnDelete(DeleteBehavior.Restrict);

            // Índice para el seguimiento de las solicitudes de un usuario.
            modelBuilder.Entity<SolicitudReservaDb>()
                .HasIndex(s => s.UsuarioId);

            // SQLite guarda las fechas como texto y, al leerlas, Entity Framework no sabe
            // si eran UTC. Todas las fechas del sistema se guardan en UTC, así que este
            // conversor las marca como UTC al leerlas para que no se interpreten como hora
            // local del servidor.
            var conversorUtc = new ValueConverter<DateTime, DateTime>(
                fecha => fecha,
                fecha => DateTime.SpecifyKind(fecha, DateTimeKind.Utc));

            modelBuilder.Entity<EventoDb>()
                .Property(e => e.Fecha)
                .HasConversion(conversorUtc);

            modelBuilder.Entity<SolicitudReservaDb>()
                .Property(s => s.FechaSolicitud)
                .HasConversion(conversorUtc);

            DatosIniciales.Cargar(modelBuilder);
        }
    }
}
