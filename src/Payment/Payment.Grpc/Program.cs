using Payment.Business.Services;
using Payment.Business.DTO;
using Payment.Data;
using Payment.Grpc.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();
builder.Services.AddPaymentData(builder.Configuration);
builder.Services.AddScoped<PaymentService>();
//builder.Services.AddAuthentication("Bearer")
//    .AddJwtBearer("Bearer", o =>
//    {
//        o.Authority = builder.Configuration["Auth:Authority"]; // URL del Authorization Server
//        o.Audience = "payment-api";
//    });
//builder.Services.AddAuthorization();

var app = builder.Build();
//app.UseAuthentication();
//app.UseAuthorization();

app.MapGrpcService<PaymentGrpcService>();
app.MapGet("/", () => "Payment gRPC service");
app.MapGet("/payment-test", async (string bookingId, decimal amount, PaymentService service) =>
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
