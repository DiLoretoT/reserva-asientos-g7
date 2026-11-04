using Payment.Business.Services;
using Payment.Data;
using Payment.Grpc.Services;
using Shared.Auth;
using Shared.Observability;

var builder = WebApplication.CreateBuilder(args);
builder.AddObservability("payment");

builder.Services.AddGrpc(options =>
{
    options.Interceptors.Add<TraceIdGrpcInterceptor>();
});
builder.Services.AddPaymentData(builder.Configuration);
builder.Services.AddScoped<PaymentService>();
// Autorización con Keycloak (contracts/tokens.md): AddValidacionKeycloak() de Shared.Auth valida
// firma, emisor, audiencia (payment-api) y vencimiento, con la sección "Keycloak" del appsettings.
builder.AddValidacionKeycloak();

var app = builder.Build();
app.UseTraceId();
app.UseAuthentication();
app.UseAuthorization();

app.MapGrpcService<PaymentGrpcService>();
app.MapGet("/", () => "Payment gRPC service");

app.Run();
