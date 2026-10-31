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
            await _logger.LogAsync("Procesando pago", $"BookingId: {request.BookingId}, Amount: {request.Amount}, Status: {request.PaymentStatus}");

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

                await _logger.LogAsync("Actualizando su estado", paymentResult.PaymentStatus == PaymentStatus.Approved ? "PagoAprobado" : "PagoRechazado", payment.BookingId);

                return new ProcessPaymentResult
                {
                    BookingId = payment.BookingId,
                    Amount = payment.Amount,
                    PaymentStatus = paymentResult.PaymentStatus
                };

            }
            catch (Exception ex)
            {
                await _logger.LogAsync("Error al procesar el pago", ex.Message, request.BookingId);
                throw;
            }
        }
    }
}
