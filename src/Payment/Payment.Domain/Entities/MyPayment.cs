
namespace Payment.Domain.Entities
{
    public class MyPayment
    {
        //Atributos del pago
        public int Id { get; set; }
        public string? BookingId { get; set; }
        public string? PaymentId { get; set; } = "P-" + Guid.NewGuid();
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "ARS";
        public PaymentStatus PaymentStatus { get; set; }
        private DateTime PaymentDate { get; set; }
    }
}
