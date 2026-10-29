using Payment.Application.Models;
using Payment.Application.Interfaces;

namespace Payment.Infrastructure.Persistance
{
    public class PaymentRepository(PaymentDbContext dbContext) : IPaymentRepository
    {
        private readonly PaymentDbContext _dbContext = dbContext;

        public async Task SaveAsync(Payment payment)
        {
            //Guarda un pago en la base de datos
            _dbContext.Payments.Add(payment);
            await _dbContext.SaveChangesAsync();
        }
    }
}
