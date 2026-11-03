using Availability.Controllers.Eventos;
using Microsoft.AspNetCore.Mvc;

namespace Availability.Web.Controllers
{
    // Controller de ASP.NET Core MVC, en el anillo 4. Recibe el pedido HTTP, delega en los
    // controllers del anillo 3 y elige la vista, como TodosApiController en Agenda.ApiRest
    // del ejemplo de la cátedra. Así ASP.NET Core queda solo en Availability.Web.
    //
    // Las dos páginas son públicas y no llevan [Authorize]: el paso 1 del enunciado permite
    // ver eventos y asientos sin iniciar sesión.
    public class EventosController : Controller
    {
        private readonly ListarEventosController _listarEventos;
        private readonly ListarAsientosController _listarAsientos;

        public EventosController(ListarEventosController listarEventos, ListarAsientosController listarAsientos)
        {
            _listarEventos = listarEventos;
            _listarAsientos = listarAsientos;
        }

        // GET / y GET /Eventos. Es la página de inicio porque la ruta por defecto de
        // Program.cs apunta a este controller.
        public async Task<IActionResult> Index()
        {
            return View(await _listarEventos.ListarEventos());
        }

        // GET /Eventos/Asientos/{id}. Si el id no es un Guid válido llega Guid.Empty, que no
        // corresponde a ningún evento y termina en la página de "no encontrado".
        public async Task<IActionResult> Asientos(Guid id)
        {
            var viewModel = await _listarAsientos.ListarAsientos(id);

            // La página se muestra con el mensaje del ViewModel, pero con código 404 para que
            // el navegador y los buscadores sepan que el evento no existe.
            if (!viewModel.EventoEncontrado)
            {
                Response.StatusCode = StatusCodes.Status404NotFound;
            }

            return View(viewModel);
        }
    }
}
