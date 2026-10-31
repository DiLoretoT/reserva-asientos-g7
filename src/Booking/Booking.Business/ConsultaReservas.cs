using Booking.Data;
using Booking.Domain;
using Microsoft.EntityFrameworkCore;

namespace Booking.Business;

// Capa de negocio de Booking (N-Layer). Tiene las reglas de las dos consultas y es la única que define cuándo un
// asiento está ocupado: confirmado, o retenido con el vencimiento todavía sin llegar. Usa el DbContext directo, 
// porque EF Core ya es la capa de datos y otra capa encima solo duplicaría código. Lo recibe
// por el constructor para no depender de cómo se crea la conexión. Solo lee, por eso usa AsNoTracking.
// El vencimiento se compara con la hora de la base de datos (UtcNow dentro de la consulta se traduce a now() en
// SQL), así no depende del reloj de cada réplica.
public class ConsultaReservas(BookingDbContext db)
{
    // Devuelve los asientos ocupados del evento. Solo informa los ocupados porque Booking no sabe qué asientos
    // existen, que resta y calcula los libres. Sin ocupados devuelve una lista vacía.
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

    // Devuelve el estado de la solicitud y el motivo si fue rechazada, o null si Booking todavía no la procesó.
    public async Task<EstadoSolicitud?> ObtenerEstadoSolicitudAsync(Guid solicitudId)
    {
        // Se calcula "vencida" dentro de la consulta para que use la hora de la base.
        var reserva = await db.Reservas
            .AsNoTracking()
            .Where(r => r.SolicitudId == solicitudId)
            .Select(r => new
            {
                r.AsientoId,
                r.Estado,
                r.Motivo,
                // Una retenida que ya venció se informa como vencida aunque el proceso de vencimiento no haya corrido.
                Vencida = r.Estado == EstadoReserva.Retenida && r.FechaVencimiento <= DateTime.UtcNow
            })
            .FirstOrDefaultAsync();

        if (reserva is null)
            return null;

        var estado = reserva.Vencida ? EstadoReserva.Vencida : reserva.Estado;
        return new EstadoSolicitud(solicitudId, reserva.AsientoId, estado, reserva.Motivo);
    }
}
