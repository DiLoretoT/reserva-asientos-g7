using Payment.Application.Models;

namespace Payment.Application.Interfaces
{
    public interface IPaymentRepository
    {
        //Define como guardar un pago en la base de datos
        Task SaveAsync(Models.Payment payment);
    }
}
