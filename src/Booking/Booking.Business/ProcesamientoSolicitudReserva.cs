using Booking.Data;
using Booking.Data.Payment;
using Booking.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Booking.Business;

public class ProcesamientoSolicitudReserva(BookingDbContext db, RetencionReservas retencion,
    IClientePayment payment, IOptions<BookingOptions> opciones, ILogger<ProcesamientoSolicitudReserva> logger)
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

        var duplicado = existente is not null;
        var situacion = await ConsultarAsync(reserva.Id, cancellationToken);
        if (situacion.Estado != EstadoReserva.Retenida || situacion.Vencida)
            return Finalizada(situacion, duplicado);

        var pago = await payment.CobrarAsync(reserva.Id, opciones.Value.MontoReserva, cancellationToken);
        if (pago == ResultadoPago.Incierto || (duplicado && pago == ResultadoPago.NoDisponible))
            return new(EstadoProcesamientoReserva.PendientePago, duplicado);

        var estadoFinal = pago == ResultadoPago.Aprobado ? EstadoReserva.Confirmada : EstadoReserva.Rechazada;
        MotivoRechazo? motivo = pago switch
        {
            ResultadoPago.Rechazado => MotivoRechazo.PagoFallido,
            ResultadoPago.NoDisponible => MotivoRechazo.PagoNoDisponible,
            _ => null
        };

        try
        {
            var actualizadas = await db.Reservas
                .Where(r => r.Id == reserva.Id && r.Estado == EstadoReserva.Retenida
                    && r.FechaVencimiento > DateTime.UtcNow)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(r => r.Estado, estadoFinal)
                    .SetProperty(r => r.Motivo, motivo)
                    .SetProperty(r => r.FechaActualizacion, r => DateTime.UtcNow), cancellationToken);

            if (actualizadas == 1)
                return new(estadoFinal == EstadoReserva.Confirmada
                    ? EstadoProcesamientoReserva.Finalizada : EstadoProcesamientoReserva.Rechazada, duplicado, motivo);

            situacion = await ConsultarAsync(reserva.Id, cancellationToken);
            if (situacion.Estado == EstadoReserva.Retenida && !situacion.Vencida)
                return new(EstadoProcesamientoReserva.PendientePago, duplicado);

            var inconsistente = pago == ResultadoPago.Aprobado && situacion.Estado != EstadoReserva.Confirmada;
            if (inconsistente)
                logger.LogWarning("Pago aprobado sin poder confirmar la reserva {ReservaId}; requiere conciliacion y devolucion no implementadas.", reserva.Id);
            return Finalizada(situacion, duplicado) with { PagoInconsistente = inconsistente };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception)
        {
            // El pago pudo realizarse, incluso si falló el guardado o la comprobación posterior.
            return new(EstadoProcesamientoReserva.PendientePago, duplicado);
        }
    }

    private Task<SituacionReserva> ConsultarAsync(Guid id, CancellationToken cancellationToken)
        => db.Reservas.AsNoTracking().Where(r => r.Id == id)
            .Select(r => new SituacionReserva(r.Estado, r.Motivo,
                r.Estado == EstadoReserva.Retenida && r.FechaVencimiento <= DateTime.UtcNow))
            .SingleAsync(cancellationToken);

    private static ResultadoProcesamientoReserva Finalizada(SituacionReserva situacion, bool duplicado)
    {
        var estado = situacion.Estado switch
        {
            EstadoReserva.Retenida when situacion.Vencida => EstadoProcesamientoReserva.Finalizada,
            EstadoReserva.Rechazada => EstadoProcesamientoReserva.Rechazada,
            EstadoReserva.Confirmada or EstadoReserva.Vencida => EstadoProcesamientoReserva.Finalizada,
            _ => throw new InvalidOperationException("Estado de reserva no contemplado.")
        };
        return new(estado, duplicado, situacion.Motivo);
    }

    private record SituacionReserva(EstadoReserva Estado, MotivoRechazo? Motivo, bool Vencida);
}
