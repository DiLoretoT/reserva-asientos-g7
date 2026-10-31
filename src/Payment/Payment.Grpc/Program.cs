using Payment.Business.Services;
using Payment.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();
builder.Services.AddPaymentData(builder.Configuration);
builder.Services.AddScoped<PaymentService>();

var app = builder.Build();

app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client.");

app.Run();
