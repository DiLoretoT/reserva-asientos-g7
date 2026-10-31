
namespace Payment.Domain.Entities
{
    public class MyPayment
    {
        //Atributos del pago
        public string? BookingId { get; set; }
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "ARS";
        public PaymentStatus PaymentStatus { get; set; }
        private DateTime PaymentDate { get; set; }
    }
}
