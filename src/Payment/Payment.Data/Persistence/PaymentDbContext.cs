using Microsoft.EntityFrameworkCore;
using Payment.Domain.Entities;

namespace Payment.Data.Persistance
{
    public class PaymentDbContext(DbContextOptions<PaymentDbContext> options) : DbContext(options)
    {
        public DbSet<MyPayment> Payment => Set<MyPayment>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    }
}