using Availability.Presenters.ListarEventos;
using Availability.UseCases.Eventos.ListarEventos.Interfaces;
using Availability.UseCases.Eventos.ListarEventos.Models;

namespace Availability.Controllers.Eventos
{
    // Controller del caso de uso ListarEventos en el anillo 3, con la misma forma que
    // ListarAsientosController: arma el InputDataRequest, ejecuta el caso de uso y devuelve el
    // ViewModel. No depende de ASP.NET Core; la ruta y la vista las resuelve Availability.Web.
    public class ListarEventosController
    {
        private readonly IInputPort _interactor;
        private readonly ListarEventosPresenter _presenter;

        public ListarEventosController(IInputPort interactor, ListarEventosPresenter presenter)
        {
            _interactor = interactor;
            _presenter = presenter;
        }

        public async Task<ListarEventosViewModel> ListarEventos()
        {
            await _interactor.HandleAsync(new InputDataRequest());

            return _presenter.ViewModel
                ?? throw new InvalidOperationException("El caso de uso no entregó un resultado al presenter.");
        }
    }
}
