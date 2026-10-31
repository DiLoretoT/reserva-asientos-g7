// Matías De La Rosa
using Availability.Controllers.Solicitudes;
using Availability.Presenters.SolicitarReserva;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Availability.Web.Controllers
{
    // Controller de ASP.NET Core MVC para la solicitud de reserva (issue #4), en el anillo 4,
    // como EventosController. Recibe el formulario, resuelve quién es el usuario, delega en
    // SolicitarReservaController del anillo 3 y elige qué página mostrar.
    //
    // [Authorize] en toda la clase: para reservar hay que iniciar sesión (paso 2 del
    // enunciado). Si alguien llega sin sesión, ASP.NET Core lo manda a Keycloak.
    [Authorize]
    public class ReservasController : Controller
    {
        // Claves de TempData para pasarle a la página de solicitud recibida los datos que no
        // viajan en la dirección.
        private const string ClaveAsiento = "SolicitudRecibida.Asiento";
        private const string ClaveEvento = "SolicitudRecibida.EventoId";

        private readonly SolicitarReservaController _solicitarReserva;

        public ReservasController(SolicitarReservaController solicitarReserva)
        {
            _solicitarReserva = solicitarReserva;
        }

        // POST /Reservas/Solicitar, desde el formulario del mapa de asientos.
        // [ValidateAntiForgeryToken] comprueba el token oculto que agrega el formulario, así
        // otro sitio no puede hacer que el navegador del usuario envíe una reserva sin que él
        // lo sepa (ataque CSRF).
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Solicitar(Guid eventoId, Guid asientoId)
        {
            // El identificador del usuario es el claim "sub" del token de Keycloak
            // (contracts/tokens.md). Program.cs configura MapInboundClaims = false, así que
            // llega con ese nombre. Se resuelve acá y no en el caso de uso, que no sabe cómo
            // se autentica el usuario.
            string? usuarioId = User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(usuarioId))
            {
                // Con [Authorize] no debería pasar. Si la sesión no trae el claim, se pide
                // iniciar sesión otra vez en lugar de registrar una solicitud sin usuario.
                return Challenge();
            }

            SolicitarReservaViewModel viewModel =
                await _solicitarReserva.SolicitarReserva(eventoId, asientoId, usuarioId);

            if (!viewModel.Recibida)
            {
                // Los errores se muestran en la misma respuesta, sin redirigir: no se creó
                // nada, así que recargar la página no duplica ninguna solicitud. El código de
                // estado le indica al navegador qué pasó, como hace EventosController con el
                // evento inexistente.
                Response.StatusCode = viewModel.PuedeReintentar
                    ? StatusCodes.Status503ServiceUnavailable
                    : StatusCodes.Status404NotFound;

                return View("NoRecibida", viewModel);
            }

            // Post/Redirect/Get: después de registrar la solicitud se redirige a una página
            // que se pide con GET. Si el usuario recarga, se repite el GET y no el POST, así
            // que no se manda una segunda solicitud.
            //
            // TempData guarda datos solo hasta el siguiente pedido, justo lo que hace falta
            // para mostrar el asiento en la página a la que se redirige.
            TempData[ClaveAsiento] = viewModel.AsientoTexto;
            TempData[ClaveEvento] = viewModel.EventoId;

            return RedirectToAction(nameof(Recibida), new { id = viewModel.SolicitudId });
        }

        // GET /Reservas/Recibida/{id}. Muestra el número de solicitud y aclara que la reserva
        // está en proceso. El resultado lo informa el seguimiento (issue #5).
        [HttpGet]
        public IActionResult Recibida(Guid id)
        {
            string? asiento = TempData[ClaveAsiento] as string;

            // El serializador de TempData devuelve el Guid como Guid, no como texto, así que
            // se lee con "is Guid".
            Guid? eventoId = TempData[ClaveEvento] is Guid evento ? evento : null;

            return View(new SolicitudRecibidaViewModel(id, asiento, eventoId));
        }
    }
}
