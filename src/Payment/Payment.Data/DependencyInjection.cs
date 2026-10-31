using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Payment.Business.DTO;
using Payment.Business.Interfaces;
using Payment.Data.Audit;
using Payment.Data.Persistance;
using Payment.Data.Processing;

namespace Payment.Data
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddPaymentData(
        this IServiceCollection services, IConfiguration config)
        {
            services.AddDbContext<PaymentDbContext>(options =>
               options.UseSqlite(config.GetConnectionString("PaymentDb")));

            services.AddScoped<IPaymentRepository, PaymentRepository>();
            services.AddScoped<IPaymentProcessor, SimulatedPaymentProcess>();
            services.AddScoped<IAuditLogger, AuditLogger>();

            return services;
        }
    }
}
