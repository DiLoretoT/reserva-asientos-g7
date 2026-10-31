
namespace Payment.Domain.Entities
{
    public class AuditLog
    {
        public int Id { get; set; }
        public string? Evento { get; set; }
        public string BookingId { get; set; } = string.Empty;
        public string? Detalle { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
