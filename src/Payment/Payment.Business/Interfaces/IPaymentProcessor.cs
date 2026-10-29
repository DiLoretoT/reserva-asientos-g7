using Payment.Application.DTO;
using Payment.Application.Models;

namespace Payment.Application.Interfaces
{
    //Define la interfaz del procesador de pagos
    public interface IPaymentProcessor
    {
        Task<PaymentStatus> ProcessAsync(ProcessPaymentRequest request);
    }
}
