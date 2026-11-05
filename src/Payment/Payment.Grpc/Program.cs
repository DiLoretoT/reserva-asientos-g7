using Microsoft.EntityFrameworkCore;
using Payment.Business.Services;
using Payment.Data;
using Payment.Data.Persistence;
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

// En desarrollo la base se crea o se actualiza al arrancar, igual que en Availability y Booking.
// El archivo .db no se versiona, así que en un clon nuevo todavía no existe.
if (app.Environment.IsDevelopment())
{
    // La conexión a la base normalmente vive lo que dura un pedido. Al arrancar no hay ningún
    // pedido, así que se abre ese ámbito a mano. Se cierra solo al terminar el bloque.
    using var scope = app.Services.CreateScope();

    // Pide la clase que conecta con la base y ejecuta las migraciones que falten.
    // Si el archivo no existe, lo crea. Las migraciones ya aplicadas no se repiten.
    scope.ServiceProvider.GetRequiredService<PaymentDbContext>().Database.Migrate();
}
app.UseTraceId();
app.UseAuthentication();
app.UseAuthorization();

app.MapGrpcService<PaymentGrpcService>();
app.MapGet("/", () => "Payment gRPC service");

app.Run();
