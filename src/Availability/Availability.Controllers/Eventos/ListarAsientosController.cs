// Tomás Di Loreto
using Availability.Presenters.ListarAsientosDisponibles;
using Availability.UseCases.Eventos.ListarAsientosDisponibles.Interfaces;
using Availability.UseCases.Eventos.ListarAsientosDisponibles.Models;

namespace Availability.Controllers.Eventos
{
    // Controller del caso de uso en el anillo 3. Arma el InputDataRequest, ejecuta el caso de
    // uso por el puerto de entrada y devuelve el ViewModel que dejó el presenter. Es una clase
    // común y no un controller de ASP.NET Core, así este proyecto no depende del framework
    // web. HTTP, rutas y vistas quedan en Availability.Web, en el anillo 4.
    public class ListarAsientosController
    {
        private readonly IInputPort _interactor;
        private readonly ListarAsientosPresenter _presenter;

        // Recibe el presenter concreto y no IOutputPort porque necesita leer su ViewModel.
        // Es la misma instancia que recibe el Interactor como IOutputPort, según el registro
        // en Program.cs.
        public ListarAsientosController(IInputPort interactor, ListarAsientosPresenter presenter)
        {
            _interactor = interactor;
            _presenter = presenter;
        }

        public async Task<ListarAsientosViewModel> ListarAsientos(Guid eventoId)
        {
            await _interactor.HandleAsync(new InputDataRequest { EventoId = eventoId });

            // El Interactor siempre entrega un resultado, con el evento encontrado o no. Si el
            // ViewModel sigue en null es un error de programación, y se lanza una excepción
            // en lugar de mostrar una página vacía.
            return _presenter.ViewModel
                ?? throw new InvalidOperationException("El caso de uso no entregó un resultado al presenter.");
        }
    }
}