using Payment.Application.Models;

namespace Payment.Application.DTO
{
    public class ProcessPaymentRequest
    {
        //Son los atributos que se van a recibir desde el front para procesar un pago
        public required string BookingId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "ARS";
        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
    }
}
