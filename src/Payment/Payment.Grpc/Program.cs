using Payment.Business.Services;
using Payment.Business.DTO;
using Payment.Data;
using Payment.Grpc.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();
builder.Services.AddPaymentData(builder.Configuration);
builder.Services.AddScoped<PaymentService>();

var app = builder.Build();

app.MapGrpcService<PaymentGrpcService>();
app.MapGet("/", () => "Payment gRPC service");
app.MapGet("/test-payment", async (string bookingId, decimal amount, PaymentService service) =>
{
    var result = await service.ProcessPayment(
        new ProcessPaymentRequest
        {
            BookingId = bookingId,
            Amount = amount
        });
    return Results.Ok(result);
});

app.Run();
