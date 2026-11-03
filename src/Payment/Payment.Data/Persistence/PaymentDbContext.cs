using Microsoft.EntityFrameworkCore;
using Payment.Domain.Entities;

namespace Payment.Data.Persistence
{
    public class PaymentDbContext(DbContextOptions<PaymentDbContext> options) : DbContext(options)
    {
        public DbSet<MyPayment> Payment => Set<MyPayment>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<MyPayment>(e =>
            {
                e.HasIndex(p => p.BookingId).IsUnique();
                e.Property(p => p.BookingId).IsRequired();
                e.Property(p => p.PaymentStatus).HasConversion<string>();
            });
        }

    }
}