using Availability.Controllers.Eventos;
using Availability.Controllers.Solicitudes;
using Availability.Gateways;
using Availability.Presenters.ListarAsientosDisponibles;
using Availability.Presenters.ListarEventos;
using Availability.Presenters.SolicitarReserva;
using ListarAsientos = Availability.UseCases.Eventos.ListarAsientosDisponibles;
using ListarEventos = Availability.UseCases.Eventos.ListarEventos;
using SolicitarReserva = Availability.UseCases.Solicitudes.SolicitarReserva;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Inicio de sesión con Keycloak (issue #6). Va en Availability.Web, el anillo 4, porque 
// la cookie y OpenID Connect son mecanismos de ASP.NET Core. Ningún caso de uso sabe 
// cómo se autentica el usuario.
// Hay dos esquemas. Uno es que la cookie guarda la sesión ya iniciada, así Availability 
// no consulta a Keycloak en cada pedido. Y el otro es OpenID Connect que actúa solo 
// cuando hay que iniciar sesión, llevando al usuario a la página de login de Keycloak y,
// con el authorization code que vuelve, pide los tokens y arma la cookie.
var keycloak = builder.Configuration.GetSection("Keycloak");
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    })
    .AddCookie()
    .AddOpenIdConnect(options =>
    {
        options.Authority = keycloak["Authority"];
        options.ClientId = keycloak["ClientId"];
        // El secret sale de User Secrets, como pide el contrato de tokens.
        options.ClientSecret = keycloak["ClientSecret"];
        options.ResponseType = "code";

        // En desarrollo Keycloak corre en HTTP (contracts/tokens.md).
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();

        // Guarda el id_token en la cookie. Al cerrar sesión se manda a Keycloak como
        // id_token_hint, que Keycloak pide para volver al sitio sin una pantalla de confirmación.
        options.SaveTokens = true;
        // Pide los datos del usuario al endpoint userinfo de Keycloak, que informa
        // preferred_username aunque el id_token no lo traiga.
        options.GetClaimsFromUserInfoEndpoint = true;

        // Deja los claims con los nombres de Keycloak (preferred_username, sub) en lugar de
        // traducirlos a los nombres largos de Microsoft. preferred_username pasa a ser
        // User.Identity.Name, que es lo que muestra el layout ("usuario1").
        options.MapInboundClaims = false;
        options.TokenValidationParameters.NameClaimType = "preferred_username";
    });

// Base de datos propia de Availability (SQLite), sus repositorios, la consulta de
// ocupación a Booking y la publicación en el broker. Booking:SimularSinRespuesta y
// Broker:SimularSinRespuesta valen false si no están definidos.
builder.Services.AddGateways(
    builder.Configuration.GetConnectionString("Availability")!,
    builder.Configuration.GetValue<bool>("Booking:SimularSinRespuesta"),
    builder.Configuration.GetValue<bool>("Broker:SimularSinRespuesta"));

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

// Caso de uso SolicitarReserva (issue #4), con el mismo registro que los anteriores y el
// alias SolicitarReserva.
builder.Services.AddTransient<SolicitarReserva.Interfaces.IInputPort, SolicitarReserva.Interactor>();
builder.Services.AddTransient<SolicitarReservaController>();
builder.Services.AddScoped<SolicitarReservaPresenter>();
builder.Services.AddScoped<SolicitarReserva.Interfaces.IOutputPort>(sp => sp.GetRequiredService<SolicitarReservaPresenter>());

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

// UseAuthentication lee la cookie y arma el usuario antes de que UseAuthorization decida si
// el pedido pasa. Sin cookie el usuario queda anónimo, y las páginas sin [Authorize]
// (eventos y asientos) se ven igual.
app.UseAuthentication();
app.UseAuthorization();
app.MapStaticAssets();

// La página de inicio es la lista de eventos, por eso el controller por defecto es Eventos.
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Eventos}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
