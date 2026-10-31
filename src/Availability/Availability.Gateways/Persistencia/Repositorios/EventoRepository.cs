using Availability.Entities.Eventos;
using Availability.UseCases.Eventos;
using Microsoft.EntityFrameworkCore;

namespace Availability.Gateways.Persistencia.Repositorios
{
    // Adaptador del puerto IEventoRepository (Ports & Adapters): implementa con Entity
    // Framework y SQLite lo que el caso de uso pide en términos de dominio.
    public class EventoRepository : IEventoRepository
    {
        private readonly AvailabilityDbContext _db;

        public EventoRepository(AvailabilityDbContext db)
        {
            _db = db;
        }

        public async Task<List<Evento>> ListarAsync()
        {
            // AsNoTracking: son consultas de solo lectura, así que Entity Framework no
            // necesita seguir los cambios de estos objetos. Es más liviano.
            // Sin Include de Asientos: el puerto aclara que el listado no los trae.
            var eventos = await _db.Eventos
                .AsNoTracking()
                .OrderBy(e => e.Fecha)
                .ToListAsync();

            return eventos.Select(e => e.ADominio()).ToList();
        }

        public async Task<Evento?> ObtenerConAsientosAsync(Guid eventoId)
        {
            var evento = await _db.Eventos
                .AsNoTracking()
                .Include(e => e.Asientos.OrderBy(a => a.Fila).ThenBy(a => a.Numero))
                .FirstOrDefaultAsync(e => e.Id == eventoId);

            return evento?.ADominio();
        }
    }
}
