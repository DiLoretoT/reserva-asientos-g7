using Payment.Application.Model;

namespace Payment.Application.Interface
{
    public interface IPaymentRepository
    {
        Task SaveAsync(IPaymentRepository payment);
        Task SaveAsync(Model.Payment payment);
    }
}
