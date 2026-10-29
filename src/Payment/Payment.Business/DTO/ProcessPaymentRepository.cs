using Payment.Application.Models;

namespace Payment.Application.DTO
{
    public class ProcessPaymentRepository
    {
        //Son los atributos que se van a devolver al front despues de procesar un pago
        public required string BookingId { get; set; }
        public decimal Amount { get; set; }
        public PaymentStatus PaymentStatus { get; set; }
        public string Currency { get; set; } = "ARS";
    }
}
