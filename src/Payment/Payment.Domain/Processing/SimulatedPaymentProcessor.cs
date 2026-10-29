using Payment.Application.DTO;
using Payment.Application.Interfaces;
using Payment.Application.Models;

namespace Payment.Infrastructure.Processing
{
    public class SimulatedPaymentProcessor : IPaymentProcessor
    {
        public async Task<PaymentStatus> ProcessAsync(ProcessPaymentRequest request)
        {
            //Simula el procesamiento de un pago y devuelve el estado del pago según el estado recibido en el request
            var wait = Task.Delay(1000); //Simula un retraso en el procesamiento
            await wait;
            if (request.PaymentStatus == PaymentStatus.Approved)
            {
                return  PaymentStatus.Approved;
            }
            return PaymentStatus.Rejected;
        }
    }
}
