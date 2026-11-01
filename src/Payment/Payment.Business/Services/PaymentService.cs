using Payment.Business.DTO;
using Payment.Business.Interfaces;
using Payment.Domain.Entities;


namespace Payment.Business.Services
{
    public class PaymentService(IPaymentRepository paymentRepository, IPaymentProcessor paymentProcessor, IAuditLogger auditLogger)
    {
        private readonly IPaymentRepository _paymentRepository = paymentRepository;
        private readonly IPaymentProcessor _paymentProcessor = paymentProcessor;
        private readonly IAuditLogger _logger = auditLogger;
        public async Task<ProcessPaymentResult> ProcessPayment(ProcessPaymentRequest request)
        {
            await _logger.LogAsync("Recepcion reserva", request.PaymentStatus, request.BookingId, $"Amount: {request.Amount}");

            try
            {
                var payment = new MyPayment
                {
                    BookingId = request.BookingId,
                    Amount = request.Amount,
                    PaymentStatus = PaymentStatus.Pending
                };
                await _paymentRepository.SaveAsync(payment);

                var paymentResult = await _paymentProcessor.ProcessPayment(request);

                payment.PaymentStatus = paymentResult.PaymentStatus;

                await _paymentRepository.UpdateAsync(payment);

                await _logger.LogAsync("Pago procesado", paymentResult.PaymentStatus, payment.BookingId, $"Amount: {payment.Amount}");

                return new ProcessPaymentResult
                {
                    BookingId = payment.BookingId,
                    Amount = payment.Amount,
                    PaymentStatus = paymentResult.PaymentStatus,
                    Reason = paymentResult.Reason
                };

            }
            catch (Exception ex)
            {
                await _logger.LogAsync("Error al procesar el pago", PaymentStatus.Rejected, request.BookingId, $"Error: {ex.Message}");
                throw;
            }
        }
    }
}
