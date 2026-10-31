using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Payment.Business.Services;


namespace Payment.Grpc.Services
{
   // [Authorize]
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

