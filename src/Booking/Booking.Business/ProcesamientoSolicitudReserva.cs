using Booking.Data;
using Booking.Domain;
using Microsoft.EntityFrameworkCore;

namespace Booking.Business;

public class ProcesamientoSolicitudReserva(BookingDbContext db, RetencionReservas retencion)
{
    public async Task<ResultadoProcesamientoReserva> ProcesarAsync(
        SolicitudReserva solicitud, CancellationToken cancellationToken)
    {
        var existente = await db.Reservas.AsNoTracking()
            .SingleOrDefaultAsync(r => r.SolicitudId == solicitud.SolicitudId, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        var reserva = existente ?? await retencion.RetenerORechazarAsync(solicitud);
        cancellationToken.ThrowIfCancellationRequested();

        if (reserva.EventoId != solicitud.EventoId || reserva.AsientoId != solicitud.AsientoId
            || reserva.UsuarioId != solicitud.UsuarioId)
            return new(EstadoProcesamientoReserva.Conflicto, existente is not null);

        var estado = reserva.Estado switch
        {
            EstadoReserva.Retenida => EstadoProcesamientoReserva.PendientePago,
            EstadoReserva.Rechazada => EstadoProcesamientoReserva.Rechazada,
            EstadoReserva.Confirmada or EstadoReserva.Vencida => EstadoProcesamientoReserva.Finalizada,
            _ => throw new InvalidOperationException("Estado de reserva no contemplado.")
        };
        return new(estado, existente is not null, reserva.Motivo);
    }
}
