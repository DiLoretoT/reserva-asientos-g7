// Tomás Di Loreto
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;

namespace Availability.Web.Controllers
{
    // Inicio y cierre de sesión (issue #6) en el anillo 4. Es el protocolo 
    // OpenID Connect que resuelven ASP.NET Core y Keycloak. 
    public class CuentaController : Controller
    {
        private readonly ILogger<CuentaController> _logger;

        public CuentaController(ILogger<CuentaController> logger)
        {
            _logger = logger;
        }

        public async Task<IActionResult> IniciarSesion(string? returnUrl)
        {
            var destino = Url.IsLocalUrl(returnUrl) ? returnUrl : Url.Action("Index", "Eventos");

            // Se llama a ChallengeAsync acá, y no se devuelve Challenge(), porque el resultado
            // de una acción se ejecuta después de que la acción terminó, fuera de este try.
            try
            {
                await HttpContext.ChallengeAsync(
                    OpenIdConnectDefaults.AuthenticationScheme,
                    new AuthenticationProperties { RedirectUri = destino });

                // ChallengeAsync ya dejó armada la redirección a Keycloak en la respuesta.
                return new EmptyResult();
            }
            catch (Exception ex) when (EsFallaDeKeycloak(ex))
            {
                _logger.LogError(ex, "AUTH [Fin Operación] {Operacion} {Resultado} Motivo={Motivo}",
                    "iniciar_sesion", "error", "keycloak_no_disponible");

                Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                return View("InicioSesionNoDisponible");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CerrarSesion()
        {
            var destino = Url.Action("Index", "Eventos")!;

            // Primero se cierra la sesión local, que no depende de Keycloak.
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            try
            {
                await HttpContext.SignOutAsync(
                    OpenIdConnectDefaults.AuthenticationScheme,
                    new AuthenticationProperties { RedirectUri = destino });

                return new EmptyResult();
            }
            catch (Exception ex) when (EsFallaDeKeycloak(ex))
            {
                // La sesión local ya se cerró, así que el usuario sigue navegando como anónimo.
                _logger.LogError(ex, "AUTH [Fin Operación] {Operacion} {Resultado} Motivo={Motivo}",
                    "cerrar_sesion", "error", "keycloak_no_disponible");

                return LocalRedirect(destino);
            }
        }

        // InvalidOperationException (IDX20803): no se pudo leer la configuración de Keycloak.
        // HttpRequestException: la configuración ya estaba leída y falló la llamada a Keycloak.
        // TaskCanceledException: Keycloak no respondió a tiempo.
        private static bool EsFallaDeKeycloak(Exception ex) =>
            ex is InvalidOperationException or HttpRequestException or TaskCanceledException;
    }
}