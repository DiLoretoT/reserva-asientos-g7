using Payment.Business.DTO;
using Payment.Business.Interfaces;
using Payment.Domain.Entities;


namespace Payment.Data.Processing
{
        public class SimulatedPaymentProcess : IPaymentProcessor
        {
            public Task<ProcessPaymentResult> ProcessPayment(ProcessPaymentRequest request)
            {
                // Simulación: aprueba si el monto es razonable, rechaza si no
                var status = request.Amount > 0 && request.Amount <= 10000
                    ? PaymentStatus.Approved
                    : PaymentStatus.Rejected;

                return Task.FromResult(new ProcessPaymentResult
                {
                    BookingId = request.BookingId,
                    Amount = request.Amount,
                    PaymentStatus = status
                });
            }
        }
}
