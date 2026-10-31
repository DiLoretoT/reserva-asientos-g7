
using Microsoft.Extensions.Logging;
using Payment.Business.Interfaces;
using Payment.Data.Audit;
using Payment.Data.Persistance;
using Payment.Domain.Entities;

namespace Payment.Data.Audit
{
    public class AuditLogger(PaymentDbContext context) : IAuditLogger
    {
        private readonly PaymentDbContext _context = context;
        public async Task LogAsync(string evento, string bookingId, string? detalle = null)
        {
            _context.AuditLogs.Add(new AuditLog
            {
                Evento = evento,
                BookingId = bookingId,
                Detalle = detalle,
                Timestamp = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }
    }
}