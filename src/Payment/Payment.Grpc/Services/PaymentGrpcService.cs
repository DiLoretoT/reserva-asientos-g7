using Grpc.Core;
using Payment.Business.Services;
using Payment.Business.DTO;


namespace Payment.Grpc.Services
{
    public class PaymentGrpcService(PaymentService paymentService) : PaymentGrpc.PaymentGrpcBase
    {
        public override async Task<ProcessPaymentReply> ProcessPayment(
            ProcessPaymentRequest request, ServerCallContext context)
        {
            var result = await paymentService.ProcessPayment(
                new Payment.Business.DTO.ProcessPaymentRequest
                {
                    BookingId = request.BookingId,
                    Amount = (decimal)request.Amount
                });

            return new ProcessPaymentReply
            {
                BookingId = result.BookingId,
                Amount = (double)result.Amount,
                PaymentStatus = result.PaymentStatus.ToString()
            };
        }
    }
}

