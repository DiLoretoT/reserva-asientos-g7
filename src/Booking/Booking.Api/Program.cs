using Booking.Api;
using Booking.Api.Broker;
using Booking.Business;
using Booking.Data;
using Booking.Data.Payment;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Payment.Grpc;
using Shared.Auth;
using Shared.Observability;

var builder = WebApplication.CreateBuilder(args);
builder.AddObservability("booking");

builder.AddValidacionKeycloak();
builder.Services.AddOpenApi();
builder.Services.ConfigureHttpJsonOptions(options => BookingJson.Configurar(options.SerializerOptions));
builder.Services.AddControllers().AddJsonOptions(options => BookingJson.Configurar(options.JsonSerializerOptions));
builder.Services.AddOptions<BookingOptions>()
    .Bind(builder.Configuration.GetSection(BookingOptions.SectionName))
    .Validate(options => options.RetentionMinutes > 0, "Booking:RetentionMinutes debe ser mayor que cero.")
    .Validate(options => options.MontoReserva > 0, "Booking:MontoReserva debe ser mayor que cero.")
    .Validate(options => options.ExpirationIntervalSeconds > 0
        && options.ExpirationIntervalSeconds <= (uint.MaxValue - 1) / 1000,
        "Booking:ExpirationIntervalSeconds debe estar entre 1 y 4294967.")
    .ValidateOnStart();

builder.Services.AddOptions<PaymentOptions>()
    .Bind(builder.Configuration.GetSection(PaymentOptions.SectionName))
    .Validate(options => Uri.TryCreate(options.Address, UriKind.Absolute, out var uri)
        && (uri.Scheme == "http" || uri.Scheme == "https"), "Payment:Address debe ser una URL HTTP o HTTPS.")
    .Validate(options => double.IsFinite(options.DeadlineSeconds) && options.DeadlineSeconds > 0
        && options.DeadlineSeconds < 60, "Payment:DeadlineSeconds debe ser mayor que cero y menor que 60.")
    .ValidateOnStart();
builder.Services.AddGrpcClient<PaymentGrpc.PaymentGrpcClient>((sp, options) =>
    options.Address = new Uri(sp.GetRequiredService<IOptions<PaymentOptions>>().Value.Address))
    .AddTokenDeServicio("payment-api");
builder.Services.AddScoped<IClientePayment>(sp => new ClientePayment(
    sp.GetRequiredService<PaymentGrpc.PaymentGrpcClient>(),
    sp.GetRequiredService<IOptions<PaymentOptions>>(),
    token => sp.GetRequiredService<ServiceTokenProvider>().ObtenerAsync("payment-api", token)));

var connectionString = builder.Configuration.GetConnectionString("Booking")
    ?? throw new InvalidOperationException("Falta configurar ConnectionStrings:Booking.");
builder.Services.AddDbContext<BookingDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddScoped<ConsultaReservas>();
builder.Services.AddScoped<RetencionReservas>();
builder.Services.AddScoped<VencimientoReservas>();
builder.Services.AddHostedService<VencimientoReservasService>();
builder.Services.AddBookingServiceBus(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<BookingDbContext>().Database.Migrate();
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
