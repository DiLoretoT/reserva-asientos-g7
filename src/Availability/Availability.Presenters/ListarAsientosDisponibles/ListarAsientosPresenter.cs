// Tomás Di Loreto
using Availability.UseCases.Eventos.ListarAsientosDisponibles.Interfaces;
using Availability.UseCases.Eventos.ListarAsientosDisponibles.Models;

namespace Availability.Presenters.ListarAsientosDisponibles
{
    // Implementa el puerto de salida que define el anillo 2. El Interactor le entrega el
    // OutputDataResponse y el presenter lo convierte en el ViewModel de la página, así el caso
    // de uso no sabe cómo se muestra su resultado y otra interfaz podría tener su propio
    // presenter sin cambiar el Interactor.
    // Guarda el ViewModel para que el controller lo lea después de ejecutar el caso de uso.
    // Por eso se registra como Scoped en Program.cs, así el controller y el Interactor
    // comparten la misma instancia durante el pedido.
    public class ListarAsientosPresenter : IOutputPort
    {
        public ListarAsientosViewModel? ViewModel { get; private set; }

        public Task HandleAsync(OutputDataResponse input)
        {
            ViewModel = new ListarAsientosViewModel(input);
            return Task.CompletedTask;
        }
    }
}