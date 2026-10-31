using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Logging;

namespace Shared.Auth;

// DECISIÓN: toda la seguridad vive en una librería compartida y se expone como
// métodos de extensión. Así cada microservicio agrega 1 línea en su Program.cs
// y no repite (ni rompe) la configuración. Un cambio de política de seguridad
// se hace en un solo lugar (principio DRY).
public static class KeycloakExtensions
{
    // FUNCIÓN 1: para APIs (Booking, Payment). Validan el token que les llega.
    public static WebApplicationBuilder AddValidacionKeycloak(this WebApplicationBuilder builder)
    {
        var opts = builder.Configuration
            .GetSection(KeycloakOptions.Seccion).Get<KeycloakOptions>()!;

        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(o =>
            {
                o.Authority = opts.Authority;       // de acá descarga las claves públicas para verificar la firma
                o.RequireHttpsMetadata = false;     // SOLO desarrollo: Keycloak corre en http
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    // Firma: garantiza que el token lo emitió Keycloak y nadie lo modificó.
                    ValidateIssuerSigningKey = true,

                    // Emisor: rechaza tokens de otro servidor de autorización.
                    ValidateIssuer = true,
                    ValidIssuer = opts.Authority,

                    // Audiencia: un token emitido para booking-api no sirve en payment-api.
                    // Limita el daño si un token se filtra (mínimo privilegio).
                    ValidateAudience = true,
                    ValidAudience = opts.Audience,

                    // Vencimiento: un token robado deja de servir solo.
                    ValidateLifetime = true
                };
                // AUDITORÍA: dejamos registro de cada decisión de seguridad.
                // Se loguea quién y por qué, pero NUNCA el token completo (si el log se filtra, el token sería usable).
                o.Events = new JwtBearerEvents
                {
                    OnTokenValidated = ctx =>
                    {
                        var log = ctx.HttpContext.RequestServices
                            .GetRequiredService<ILoggerFactory>().CreateLogger("Shared.Auth.Audit");

                        // Usuario real: preferred_username. Token de servicio: azp (client que lo pidió).
                        var quien = ctx.Principal?.FindFirst("preferred_username")?.Value
                                    ?? ctx.Principal?.FindFirst("azp")?.Value
                                    ?? "desconocido";

                        log.LogInformation("AUTH token aceptado. Quien={Quien} Endpoint={Metodo} {Ruta}",
                            quien, ctx.Request.Method, ctx.Request.Path);
                        return Task.CompletedTask;
                    },

                    OnAuthenticationFailed = ctx =>
                    {
                        var log = ctx.HttpContext.RequestServices
                            .GetRequiredService<ILoggerFactory>().CreateLogger("Shared.Auth.Audit");

                        // Traducimos la excepción técnica a un motivo entendible.
                        var motivo = ctx.Exception switch
                        {
                            SecurityTokenExpiredException => "token vencido",
                            SecurityTokenInvalidSignatureException => "firma inválida",
                            SecurityTokenInvalidAudienceException => "audiencia incorrecta",
                            SecurityTokenInvalidIssuerException => "emisor incorrecto",
                            _ => "token inválido"
                        };

                        log.LogWarning("AUTH token rechazado. Motivo={Motivo} Endpoint={Metodo} {Ruta}",
                            motivo, ctx.Request.Method, ctx.Request.Path);
                        return Task.CompletedTask;
                    },

                    OnChallenge = ctx =>
                    {
                        // OnChallenge también se dispara después de un fallo ya logueado arriba.
                        // Solo registramos acá el caso en que directamente no llegó token.
                        if (ctx.AuthenticateFailure is null)
                        {
                            var log = ctx.HttpContext.RequestServices
                                .GetRequiredService<ILoggerFactory>().CreateLogger("Shared.Auth.Audit");

                            log.LogWarning("AUTH solicitud rechazada. Motivo=sin token Endpoint={Metodo} {Ruta}",
                                ctx.Request.Method, ctx.Request.Path);
                        }
                        return Task.CompletedTask;
                    }
                };
            });

        builder.Services.AddAuthorization();
        return builder;   // se devuelve el builder para poder encadenar llamadas
    }

    // FUNCIÓN 2: para el sitio MVC (Availability). Login de usuarios con Keycloak.
    public static WebApplicationBuilder AddLoginKeycloak(this WebApplicationBuilder builder)
    {
        var opts = builder.Configuration
            .GetSection(KeycloakOptions.Seccion).Get<KeycloakOptions>()!;

        builder.Services
            .AddAuthentication(o =>
            {
                // Cookie: mantiene la sesión del usuario en el sitio.
                o.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                // OIDC: cuando hace falta login, redirige a Keycloak.
                o.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            })
            .AddCookie()
            .AddOpenIdConnect(o =>
            {
                o.Authority = opts.Authority;
                o.ClientId = opts.ClientId;
                o.ClientSecret = opts.ClientSecret;

                // Authorization Code Flow: el navegador nunca ve el token, el servidor
                // lo canjea por detrás con el secret. Es el flujo recomendado para sitios web.
                o.ResponseType = "code";

                o.SaveTokens = true;                // guarda los tokens en la sesión
                o.RequireHttpsMetadata = false;     // SOLO desarrollo
                o.GetClaimsFromUserInfoEndpoint = true;
                o.Scope.Add("openid");
                o.Scope.Add("profile");
            });

        builder.Services.AddAuthorization();
        return builder;
    }
}

// FUNCIÓN 3: token de servicio (autenticación entre microservicios).
public static class TokenDeServicioExtensions
{
    // DECISIÓN: se extiende IHttpClientBuilder (no WebApplicationBuilder) porque
    // tanto AddHttpClient (REST) como AddGrpcClient (gRPC) devuelven ese tipo:
    // la misma función sirve para los dos.
    public static IHttpClientBuilder AddTokenDeServicio(this IHttpClientBuilder builder)
    {
        builder.Services.AddHttpClient();   // asegura IHttpClientFactory para pedir el token
        builder.Services.TryAddSingleton<ServiceTokenProvider>();   // Singleton: el caché se comparte entre requests
        builder.Services.TryAddTransient<TokenDeServicioHandler>();
        return builder.AddHttpMessageHandler<TokenDeServicioHandler>();
    }
}

// Pide el token con client credentials (el servicio se identifica a sí mismo, sin usuario)
// y lo CACHEA: pedirlo en cada llamada sumaría una ida a Keycloak por request
// (latencia, carga y un punto de falla más). Se renueva 30 s antes de vencer.
public class ServiceTokenProvider(IConfiguration config, IHttpClientFactory factory, ILogger<ServiceTokenProvider> log)
{
    private readonly KeycloakOptions _opts =
        config.GetSection(KeycloakOptions.Seccion).Get<KeycloakOptions>()!;
    private readonly SemaphoreSlim _lock = new(1, 1);   // evita que 2 requests simultáneos pidan token a la vez
    private string? _token;
    private DateTimeOffset _expira;

    public async Task<string> ObtenerAsync(CancellationToken ct)
    {
        if (_token is not null && DateTimeOffset.UtcNow < _expira) return _token;

        await _lock.WaitAsync(ct);
        var client = factory.CreateClient();

        log.LogInformation("AUTH pidiendo token de servicio. ClientId={ClientId}", _opts.ClientId);
        try
        {
            var resp = await client.PostAsync(
                $"{_opts.Authority}/protocol/openid-connect/token",
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "client_credentials",
                    ["client_id"] = _opts.ClientId,
                    ["client_secret"] = _opts.ClientSecret
                }), ct);
            resp.EnsureSuccessStatusCode();

            var data = (await resp.Content.ReadFromJsonAsync<TokenResponse>(ct))!;
            _token = data.AccessToken;
            _expira = DateTimeOffset.UtcNow.AddSeconds(data.ExpiresIn - 30);

            log.LogInformation("AUTH token de servicio obtenido. ClientId={ClientId} VenceEnSegundos={Seg}",
                _opts.ClientId, data.ExpiresIn);
            return _token;
        }
        catch (Exception ex)
        {
            log.LogError("AUTH falló el pedido de token de servicio. ClientId={ClientId} Error={Error}",
                _opts.ClientId, ex.Message);
            throw;
        }
        finally { _lock.Release(); }
    }

    private record TokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn);
}

// Patrón Decorator/Chain of Responsibility: un DelegatingHandler se ubica en la
// cadena de HttpClient y agrega el header Authorization a cada llamada saliente,
// sin que el código de negocio tenga que acordarse de hacerlo.
public class TokenDeServicioHandler(ServiceTokenProvider provider) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken ct)
    {
        var token = await provider.ObtenerAsync(ct);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await base.SendAsync(request, ct);
    }
}