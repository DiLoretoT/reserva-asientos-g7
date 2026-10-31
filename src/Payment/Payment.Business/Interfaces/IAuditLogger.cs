namespace Payment.Business.Interfaces
{
    public interface IAuditLogger
    {
        Task LogAsync(string evento, string bookingId, string? detalle = null);
    }
}