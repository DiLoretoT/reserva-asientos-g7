using Payment.Business.Interfaces;
using Payment.Domain.Entities;

namespace Payment.Data.Persistence
{
    public class PaymentRepository(PaymentDbContext dbContext) : IPaymentRepository
    {
        private readonly PaymentDbContext _dbContext = dbContext;

        public async Task SaveAsync(MyPayment MyPayment)
        {

            _dbContext.Payment.Add(MyPayment);
            await _dbContext.SaveChangesAsync();
        }

        public async Task UpdateAsync(MyPayment payment)
        {
            _dbContext.Payment.Update(payment);
            await _dbContext.SaveChangesAsync();
        }
    }
}
