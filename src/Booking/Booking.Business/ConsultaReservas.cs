using Booking.Data;
using Booking.Domain;
using Microsoft.EntityFrameworkCore;

namespace Booking.Business;

// Solo lee reservas: no crea ni modifica nada. Las dos consultas aplican el mismo criterio del
// contrato: una reserva retenida solo cuenta mientras su vencimiento no llegó, y esa hora se
// compara con la de la base de datos (DateTime.UtcNow dentro de la consulta se traduce a now() en SQL).
public class ConsultaReservas(BookingDbContext db)
{
    // Devuelve los asientos del evento que tienen una reserva confirmada o una retenida todavía vigente.
    // Si no hay ninguno, la lista viene vacía.
    public async Task<List<AsientoOcupado>> ObtenerAsientosOcupadosAsync(Guid eventoId)
    {
        return await db.Reservas
            .AsNoTracking()
            .Where(r => r.EventoId == eventoId
                && (r.Estado == EstadoReserva.Confirmada
                    || (r.Estado == EstadoReserva.Retenida && r.FechaVencimiento > DateTime.UtcNow)))
            .Select(r => new AsientoOcupado(r.AsientoId, r.Estado))
            .ToListAsync();
    }

    // Busca la reserva creada a partir de la solicitud y devuelve su estado y, si fue rechazada, el motivo.
    // Devuelve null si Booking todavía no procesó la solicitud.
    public async Task<EstadoSolicitud?> ObtenerEstadoSolicitudAsync(Guid solicitudId)
    {
        var reserva = await db.Reservas
            .AsNoTracking()
            .Where(r => r.SolicitudId == solicitudId)
            .Select(r => new
            {
                r.AsientoId,
                r.Estado,
                r.Motivo,
                // Retenida con vencimiento cumplido: se informa vencida aunque el proceso que la marca no haya corrido.
                Vencida = r.Estado == EstadoReserva.Retenida && r.FechaVencimiento <= DateTime.UtcNow
            })
            .FirstOrDefaultAsync();

        if (reserva is null)
            return null;

        var estado = reserva.Vencida ? EstadoReserva.Vencida : reserva.Estado;
        return new EstadoSolicitud(solicitudId, reserva.AsientoId, estado, reserva.Motivo);
    }
}
