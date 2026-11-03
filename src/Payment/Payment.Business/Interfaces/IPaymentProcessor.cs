using Payment.Business.DTO;

namespace Payment.Business.Interfaces
{
    public interface IPaymentProcessor
    {
        Task<ProcessPaymentResult> ProcessPayment(ProcessPaymentRequest request);
    }
}
