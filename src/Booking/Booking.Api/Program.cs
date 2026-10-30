using Booking.Api;
using Booking.Business;
using Booking.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.ConfigureHttpJsonOptions(options => BookingJson.Configurar(options.SerializerOptions));
builder.Services.AddControllers().AddJsonOptions(options => BookingJson.Configurar(options.JsonSerializerOptions));
builder.Services.AddOptions<BookingOptions>()
    .Bind(builder.Configuration.GetSection(BookingOptions.SectionName))
    .Validate(options => options.RetentionMinutes > 0, "Booking:RetentionMinutes debe ser mayor que cero.")
    .ValidateOnStart();

var connectionString = builder.Configuration.GetConnectionString("Booking")
    ?? throw new InvalidOperationException("Falta configurar ConnectionStrings:Booking.");
builder.Services.AddDbContext<BookingDbContext>(options => options.UseNpgsql(connectionString));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<BookingDbContext>().Database.Migrate();
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.Run();
