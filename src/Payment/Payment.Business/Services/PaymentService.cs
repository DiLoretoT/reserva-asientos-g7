using Payment.Application.Interfaces;
using Payment.Application.DTO;
using Payment.Infrastructure.Audit;

namespace Payment.Application.Services
{
    public class PaymentService(IPaymentRepository paymentRepository, IPaymentProcessor paymentProcessor, AuditLogger auditLogger)
    {

        //Define el servicio de pago que se encarga de procesar un pago y guardar la información en la base de datos
        private readonly IPaymentRepository _paymentRepository = paymentRepository;
        private readonly IPaymentProcessor _paymentProcessor = paymentProcessor;
        private readonly AuditLogger _auditLogger = auditLogger;

        public async Task<ProcessPaymentRepository> ProcessAsync(ProcessPaymentRequest request)
        {
            //Registra el intento de pago en el log de auditoría
            _auditLogger.LogPaymentAttempt(request.BookingId, request.Amount);

            var status = await _paymentProcessor.ProcessAsync(request);
            _auditLogger.LogPaymentResult(
                request.BookingId,
                request.Amount,
                status.ToString());

            //Crea un objeto Payment con los datos del request
            var payment = new Payment.Application.Models.Payment
            {
                BookingId = request.BookingId,
                Amount = request.Amount,
                PaymentStatus = request.PaymentStatus,
                Currency = request.Currency
            };

            //Procesa el pago y obtiene el estado del pago
            payment.PaymentStatus = await _paymentProcessor.ProcessAsync(request);

            //Guarda el pago en la base de datos
            await _paymentRepository.SaveAsync(payment);

            //Devuelve un objeto con los datos del pago procesado
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


