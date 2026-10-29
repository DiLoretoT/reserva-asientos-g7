using Availability.UseCases.Eventos.ListarEventos.Interfaces;
using Availability.UseCases.Eventos.ListarEventos.Models;

namespace Availability.Presenters.ListarEventos
{
    // Implementa el puerto de salida de ListarEventos con la misma forma que
    // ListarAsientosPresenter: guarda el ViewModel para que el controller lo lea, y por eso
    // se registra como Scoped, compartido con el Interactor durante el pedido.
    public class ListarEventosPresenter : IOutputPort
    {
        public ListarEventosViewModel? ViewModel { get; private set; }

        public Task HandleAsync(OutputDataResponse input)
        {
            ViewModel = new ListarEventosViewModel(input);
            return Task.CompletedTask;
        }
    }
}
