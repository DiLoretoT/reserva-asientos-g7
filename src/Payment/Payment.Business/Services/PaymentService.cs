using Microsoft.EntityFrameworkCore;
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
            await _logger.LogAsync("Recepcion reserva", PaymentStatus.Pending, request.BookingId, $"Amount: {request.Amount}");

            try
            {
                var existingPayment = await _paymentRepository.GetByBookingIdAsync(request.BookingId);
                if (existingPayment != null && existingPayment.PaymentStatus != PaymentStatus.Pending)
                {
                    await _logger.LogAsync("Pago duplicado ignorado", existingPayment.PaymentStatus, request.BookingId, $"Amount: {existingPayment.Amount}");
                    return new ProcessPaymentResult
                    {
                        BookingId = existingPayment.BookingId,
                        Amount = existingPayment.Amount,
                        PaymentStatus = existingPayment.PaymentStatus,
                        Reason = "Pago ya procesado"
                    };
                }

                var payment = existingPayment ?? new MyPayment
                {
                    BookingId = request.BookingId,
                    Amount = request.Amount,
                    PaymentStatus = PaymentStatus.Pending
                };

                if (existingPayment == null)
                {
                    await _paymentRepository.SaveAsync(payment);
                }

                var paymentResult = await _paymentProcessor.ProcessPayment(request);

                payment.PaymentStatus = paymentResult.PaymentStatus;

                await _paymentRepository.UpdateAsync(payment);

                await _logger.LogAsync("Pago procesado", paymentResult.PaymentStatus, request.BookingId, $"Amount: {payment.Amount}");

                return new ProcessPaymentResult
                {
                    BookingId = payment.BookingId,
                    Amount = payment.Amount,
                    PaymentStatus = paymentResult.PaymentStatus,
                    Reason = paymentResult.Reason
                };

            }
            catch (DbUpdateConcurrencyException)
            {
                var savedPayment = await _paymentRepository.GetByBookingIdAsync(request.BookingId);
                if (savedPayment != null)
                {
                    await _logger.LogAsync("Error al procesar el pago", savedPayment.PaymentStatus, request.BookingId, "Error: DbUpdateException");
                    return new ProcessPaymentResult
                    {
                        BookingId = savedPayment.BookingId,
                        Amount = savedPayment.Amount,
                        PaymentStatus = savedPayment.PaymentStatus,
                        Reason = "Pago duplicado por concurrencia"
                    };
                }
                else
                {
                    await _logger.LogAsync("Error al procesar el pago", PaymentStatus.Rejected, request.BookingId, "Error: DbUpdateException");
                    throw;
                }
            }
            catch (Exception ex)
            {
                await _logger.LogAsync("Error tecnico al procesar el pago", PaymentStatus.Rejected, request.BookingId, $"Error: {ex.Message}");
                throw;
            }
        }
    }
}
