using Availability.Controllers.Eventos;
using Availability.Gateways;
using Availability.Presenters.ListarAsientosDisponibles;
using Availability.UseCases.Eventos.ListarAsientosDisponibles;
using Availability.UseCases.Eventos.ListarAsientosDisponibles.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Base de datos propia de Availability (SQLite), sus repositorios y la consulta de
// ocupación a Booking. Booking:SimularSinRespuesta vale false si no está definido.
builder.Services.AddGateways(
    builder.Configuration.GetConnectionString("Availability")!,
    builder.Configuration.GetValue<bool>("Booking:SimularSinRespuesta"));

// Caso de uso ListarAsientosDisponibles, registrado como en el ejemplo de la cátedra.
// El Interactor y el controller son Transient porque no guardan nada entre pedidos.
builder.Services.AddTransient<IInputPort, Interactor>();
builder.Services.AddTransient<ListarAsientosController>();

// El presenter es Scoped, una instancia por pedido HTTP, y quien pide IOutputPort recibe
// esa misma instancia. Así el controller lee el ViewModel que llenó el Interactor.
builder.Services.AddScoped<ListarAsientosPresenter>();
builder.Services.AddScoped<IOutputPort>(sp => sp.GetRequiredService<ListarAsientosPresenter>());

var app = builder.Build();

// En desarrollo, la base se crea o se actualiza sola al arrancar el sitio.
if (app.Environment.IsDevelopment())
{
    app.Services.AplicarMigraciones();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
