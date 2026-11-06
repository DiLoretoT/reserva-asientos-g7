namespace Booking.Data.Payment;

public enum ResultadoPago
{
    Aprobado,
    Rechazado,
    NoDisponible,
    Incierto
}

public interface IClientePayment
{
    Task<ResultadoPago> CobrarAsync(Guid reservaId, decimal monto, CancellationToken cancellationToken);
}
