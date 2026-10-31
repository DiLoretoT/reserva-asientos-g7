using Payment.Domain.Entities;

namespace Payment.Business.DTO
{
    public class ProcessPaymentRequest
    {
        public required string BookingId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "ARS";
        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
    }
}
