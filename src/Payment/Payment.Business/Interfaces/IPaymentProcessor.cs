using Payment.Business.DTO;
using Payment.Domain.Entities;

namespace Payment.Business.Interfaces
{
    public interface IPaymentProcessor
    {
        Task<ProcessPaymentResult> ProcessPayment(ProcessPaymentRequest request);
    }
}
