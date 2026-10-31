using Payment.Domain.Entities;

namespace Payment.Business.DTO
{
    public class ProcessPaymentResult
    {
        public string BookingId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public PaymentStatus PaymentStatus { get; set; }
    }
}
