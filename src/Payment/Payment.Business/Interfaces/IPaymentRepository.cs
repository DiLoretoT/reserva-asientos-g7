using Payment.Domain.Entities;

namespace Payment.Business.Interfaces
{
    public interface IPaymentRepository
    {
        Task SaveAsync(MyPayment payment);
        Task UpdateAsync(MyPayment payment);
    }
}
