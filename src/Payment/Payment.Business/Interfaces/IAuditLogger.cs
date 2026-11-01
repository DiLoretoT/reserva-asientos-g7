using Payment.Domain.Entities;

namespace Payment.Business.Interfaces
{
    public interface IAuditLogger
    {
        Task LogAsync(string evento, PaymentStatus status, string bookingId, string? detalle = null);
    }
}