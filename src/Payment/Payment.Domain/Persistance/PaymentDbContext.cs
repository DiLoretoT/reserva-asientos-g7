using Microsoft.EntityFrameworkCore;
using Payment.Application.Models;

namespace Payment.Infrastructure.Persistance
{
    public class PaymentDbContext(DbContextOptions<PaymentDbContext> options) : DbContext(options)
    {
        public DbSet<Payment> Payments => Set<Payment>();
    }
}
