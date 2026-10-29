using Availability.Controllers.Eventos;
using Availability.Gateways;
using Availability.Presenters.ListarAsientosDisponibles;
using Availability.Presenters.ListarEventos;
using ListarAsientos = Availability.UseCases.Eventos.ListarAsientosDisponibles;
using ListarEventos = Availability.UseCases.Eventos.ListarEventos;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Base de datos propia de Availability (SQLite), sus repositorios y la consulta de
// ocupación a Booking. Booking:SimularSinRespuesta vale false si no está definido.
builder.Services.AddGateways(
    builder.Configuration.GetConnectionString("Availability")!,
    builder.Configuration.GetValue<bool>("Booking:SimularSinRespuesta"));

// Cada caso de uso tiene clases con el mismo nombre (Interactor, IInputPort, IOutputPort)
// en su propio namespace. Los alias ListarAsientos y ListarEventos de los using de arriba
// indican de qué caso de uso es cada una.
//
// En los dos casos, el Interactor y el controller son Transient porque no guardan nada
// entre pedidos. El presenter es Scoped, una instancia por pedido HTTP, y quien pide
// IOutputPort recibe esa misma instancia, así el controller lee el ViewModel que llenó
// el Interactor.

// Caso de uso ListarAsientosDisponibles.
builder.Services.AddTransient<ListarAsientos.Interfaces.IInputPort, ListarAsientos.Interactor>();
builder.Services.AddTransient<ListarAsientosController>();
builder.Services.AddScoped<ListarAsientosPresenter>();
builder.Services.AddScoped<ListarAsientos.Interfaces.IOutputPort>(sp => sp.GetRequiredService<ListarAsientosPresenter>());

// Caso de uso ListarEventos.
builder.Services.AddTransient<ListarEventos.Interfaces.IInputPort, ListarEventos.Interactor>();
builder.Services.AddTransient<ListarEventosController>();
builder.Services.AddScoped<ListarEventosPresenter>();
builder.Services.AddScoped<ListarEventos.Interfaces.IOutputPort>(sp => sp.GetRequiredService<ListarEventosPresenter>());

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

// La página de inicio es la lista de eventos, por eso el controller por defecto es Eventos.
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Eventos}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
