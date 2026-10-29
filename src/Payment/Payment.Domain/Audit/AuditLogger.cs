using Microsoft.Extensions.Logging;

namespace Payment.Infrastructure.Audit
{
    public class AuditLogger(ILogger<AuditLogger> logger)
    {
        private readonly ILogger<AuditLogger> _logger = logger;

        public void LogPaymentAttempt(string bookingId, decimal amount)
        {
            _logger.LogInformation("Payment Attempt: BookingId={BookingId}, Amount={Amount}",
                bookingId, amount);
        }
        public void LogPaymentResult(string bookingId, decimal amount, string status)
        {
            _logger.LogInformation("Payment Result: BookingId={BookingId}, Amount={Amount}, Status={Status}",
                bookingId, amount, status);
        }
        public void LogPaymentError(string bookingId, decimal amount, string errorMessage)
        {
            _logger.LogError("Payment Error: BookingId={BookingId}, Amount={Amount}, Error={ErrorMessage}",
                bookingId, amount, errorMessage);
        }
    }
}
