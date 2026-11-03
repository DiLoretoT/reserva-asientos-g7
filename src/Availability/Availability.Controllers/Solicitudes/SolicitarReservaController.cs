// Matías De La Rosa
using Availability.Presenters.SolicitarReserva;
using Availability.UseCases.Solicitudes.SolicitarReserva.Interfaces;
using Availability.UseCases.Solicitudes.SolicitarReserva.Models;

namespace Availability.Controllers.Solicitudes
{
    // Controller del caso de uso SolicitarReserva en el anillo 3, con la misma forma que
    // ListarAsientosController: arma el InputDataRequest, ejecuta el caso de uso por el puerto
    // de entrada y devuelve el ViewModel que dejó el presenter. Es una clase común y no un
    // controller de ASP.NET Core, así este proyecto no depende del framework web. El
    // formulario, el usuario logueado y la redirección quedan en Availability.Web.
    public class SolicitarReservaController
    {
        private readonly IInputPort _interactor;
        private readonly SolicitarReservaPresenter _presenter;

        // Recibe el presenter concreto y no IOutputPort porque necesita leer su ViewModel. Es
        // la misma instancia que recibe el Interactor como IOutputPort, según el registro en
        // Program.cs.
        public SolicitarReservaController(IInputPort interactor, SolicitarReservaPresenter presenter)
        {
            _interactor = interactor;
            _presenter = presenter;
        }

        // usuarioId llega ya resuelto desde Availability.Web (claim "sub" del token de
        // Keycloak). Este anillo no sabe cómo se autentica el usuario.
        public async Task<SolicitarReservaViewModel> SolicitarReserva(Guid eventoId, Guid asientoId, string usuarioId)
        {
            await _interactor.HandleAsync(new InputDataRequest
            {
                EventoId = eventoId,
                AsientoId = asientoId,
                UsuarioId = usuarioId
            });

            // El Interactor siempre entrega un resultado, también en los casos de error. Si el
            // ViewModel sigue en null es un error de programación, y se lanza una excepción en
            // lugar de mostrar una página vacía.
            return _presenter.ViewModel
                ?? throw new InvalidOperationException("El caso de uso no entregó un resultado al presenter.");
        }
    }
}
