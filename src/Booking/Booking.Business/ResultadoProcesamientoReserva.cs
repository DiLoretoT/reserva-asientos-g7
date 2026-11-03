using Booking.Domain;

namespace Booking.Business;

public enum EstadoProcesamientoReserva
{
    PendientePago,
    Finalizada,
    Rechazada,
    Conflicto
}

public record ResultadoProcesamientoReserva(
    EstadoProcesamientoReserva Estado, bool EsDuplicado, MotivoRechazo? Motivo = null,
    bool PagoInconsistente = false);
