using Payment.Application.Interface;
using Payment.Application.DTO;
namespace Payment.Application.Service
{
    public class PaymentService(IPaymentRepository paymentRepository)
    {
        private readonly IPaymentRepository _paymentRepository = paymentRepository;

        public async Task<ProcessPaymentRepository> ProcessAsync(ProcessPaymentRequest request)
        {
            var payment = new Payment.Application.Model.Payment
            {
                BookingId = request.BookingId,
                Amount = request.Amount,
                PaymentStatus = request.PaymentStatus,
                Currency = request.Currency
            };

            await _paymentRepository.SaveAsync(payment);

            return new ProcessPaymentRepository
            {
                BookingId = payment.BookingId,
                Amount = payment.Amount,
                PaymentStatus = payment.PaymentStatus,
                Currency = payment.Currency
            };
        }

        
    }
}
