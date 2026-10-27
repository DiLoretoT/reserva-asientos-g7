using Availability.Gateways;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Base de datos propia de Availability (SQLite) y sus repositorios.
builder.Services.AddGateways(builder.Configuration.GetConnectionString("Availability")!);

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
