using Availability.Entities.Solicitudes;
using Availability.UseCases.Solicitudes;
using Microsoft.EntityFrameworkCore;

namespace Availability.Gateways.Persistencia.Repositorios
{
    // Adaptador del puerto ISolicitudRepository con Entity Framework y SQLite.
    public class SolicitudRepository : ISolicitudRepository
    {
        private readonly AvailabilityDbContext _db;

        public SolicitudRepository(AvailabilityDbContext db)
        {
            _db = db;
        }

        public async Task AgregarAsync(SolicitudReserva solicitud)
        {
            _db.Solicitudes.Add(solicitud.APersistencia());

            // Se guarda en el momento y no se deja para después: el caso de uso necesita
            // saber que la solicitud quedó registrada antes de publicarla en el broker.
            await _db.SaveChangesAsync();
        }

        public async Task<SolicitudReserva?> ObtenerAsync(Guid solicitudId)
        {
            var solicitud = await _db.Solicitudes
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == solicitudId);

            return solicitud?.ADominio();
        }

        public async Task<List<SolicitudReserva>> ListarPendientesDeEnvioAsync(DateTime registradasAntesDe, int maximo)
        {
            var pendientes = await _db.Solicitudes
                .AsNoTracking()
                .Where(s => s.EstadoEnvio == EstadoEnvio.Pendiente && s.FechaSolicitud < registradasAntesDe)
                .OrderBy(s => s.FechaSolicitud)
                .Take(maximo)
                .ToListAsync();

            return pendientes.Select(s => s.ADominio()).ToList();
        }

        public async Task MarcarEnviadaAsync(Guid solicitudId)
        {
            // ExecuteUpdateAsync actualiza directo en la base con un UPDATE, sin cargar antes
            // la solicitud en memoria.
            await _db.Solicitudes
                .Where(s => s.Id == solicitudId)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.EstadoEnvio, EstadoEnvio.Enviada));
        }
    }
}
