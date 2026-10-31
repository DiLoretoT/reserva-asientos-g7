using Payment.Business.DTO;
using Payment.Business.Interfaces;
using Payment.Domain.Entities;


namespace Payment.Data.Processing
{
        public class SimulatedPaymentProcess : IPaymentProcessor
        {
            private const decimal MaxAmount = 10000m; // Monto máximo permitido para aprobar el pago
            public Task<ProcessPaymentResult> ProcessPayment(ProcessPaymentRequest request)
            {
                var (status, reason) = Evaluate(request.Amount);

                return Task.FromResult(new ProcessPaymentResult
                {
                    BookingId = request.BookingId,
                    Amount = request.Amount,
                    PaymentStatus = status,
                    Reason = reason
                });

        }
            private static (PaymentStatus status, string? reason) Evaluate(decimal amount)
            {
                if (amount <= 0)
                {
                return (PaymentStatus.Rejected, "Monto invalido");
                }
                if (amount > MaxAmount)
                {
                    return (PaymentStatus.Rejected, "Monto excede el límite permitido");
                }
                return (PaymentStatus.Approved, "Pago aprobado con exito");
            }
        }
}
