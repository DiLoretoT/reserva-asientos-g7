using Payment.Business.Services;
using Payment.Business.DTO;
using Payment.Data;
using Payment.Grpc.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();
builder.Services.AddPaymentData(builder.Configuration);
builder.Services.AddScoped<PaymentService>();
// Autorización con Keycloak (contracts/tokens.md): AddValidacionKeycloak() de Shared.Auth valida
// firma, emisor, audiencia (payment-api) y vencimiento, con la sección "Keycloak" del appsettings.
// Queda comentado hasta la integración: si Payment exige token antes de que Booking lo mande
// con AddTokenDeServicio("payment-api"), la llamada Booking -> Payment responde Unauthenticated.
//builder.AddValidacionKeycloak();

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
