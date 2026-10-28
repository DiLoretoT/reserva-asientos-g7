
namespace Payment.Application.Model
{
    public class Payment
    {
        private int PaymentId { get; set; }
        public string BookingId { get; set; }= string.Empty;
        //public string Subject { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "ARS";
        //public string PaymentMethod { get; set; }
        private DateTime PaymentDate { get; set; } = DateTime.Now;
        public string PaymentStatus { get; set; } = string.Empty;
    }
}
