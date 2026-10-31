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
        public IActionResult IniciarSesion(string? returnUrl)
        {
            var destino = Url.IsLocalUrl(returnUrl) ? returnUrl : Url.Action("Index", "Eventos");

            return Challenge(
                new AuthenticationProperties { RedirectUri = destino },
                OpenIdConnectDefaults.AuthenticationScheme);
        }
    [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CerrarSesion()
        {
            return SignOut(
                new AuthenticationProperties { RedirectUri = Url.Action("Index", "Eventos") },
                CookieAuthenticationDefaults.AuthenticationScheme,
                OpenIdConnectDefaults.AuthenticationScheme);
        }
    }
}