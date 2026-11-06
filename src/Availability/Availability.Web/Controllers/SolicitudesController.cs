// Tomás Di Loreto
using Availability.Controllers.Solicitudes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Availability.Web.Controllers
{
    // Controller de ASP.NET Core para el seguimiento de solicitudes, en el anillo 4.
    //
    // Authorize pide la sesión iniciada. Sin cookie, ASP.NET Core manda al usuario a la página
    // de login de Keycloak (DefaultChallengeScheme de Program.cs) y, al volver, lo deja en esta
    // misma página. Solo protege el acceso, de forma que cada usuario vea únicamente sus solicitudes lo
    // decide el caso de uso.
    [Authorize]
    public class SolicitudesController : Controller
    {
        private readonly ConsultarEstadoSolicitudController _consultarEstado;

        public SolicitudesController(ConsultarEstadoSolicitudController consultarEstado)
        {
            _consultarEstado = consultarEstado;
        }

        // GET /solicitudes/{id}. Usa una ruta de atributo porque la ruta por defecto de
        // Program.cs exigiría /Solicitudes/Detalle/{id}. Si el id no es un Guid llega
        // Guid.Empty, que no corresponde a ninguna solicitud.
        [HttpGet("solicitudes/{id}")]
        public async Task<IActionResult> Detalle(Guid id)
        {
            // sub es el identificador del usuario en Keycloak, el mismo que se guarda como
            // UsuarioId en la solicitud. Llega con ese nombre porque Program.cs desactiva la
            // traducción de claims (MapInboundClaims = false). Keycloak lo incluye siempre, así
            // que si falta es un error de configuración y no un caso a mostrar.
            string usuarioId = User.FindFirst("sub")?.Value
                ?? throw new InvalidOperationException("La sesión no tiene el claim sub.");

            var viewModel = await _consultarEstado.ConsultarEstado(id, usuarioId);

            // Igual que en la página de asientos: se muestra el mensaje, con código 404.
            if (!viewModel.SolicitudEncontrada)
            {
                Response.StatusCode = StatusCodes.Status404NotFound;
            }

            return View(viewModel);
        }
    }
}