// Matías De La Rosa
using Availability.UseCases.Solicitudes.SolicitarReserva.Interfaces;
using Availability.UseCases.Solicitudes.SolicitarReserva.Models;

namespace Availability.Presenters.SolicitarReserva
{
    // Implementa el puerto de salida de SolicitarReserva con la misma forma que los presenters
    // de Eventos: el Interactor le entrega el OutputDataResponse y el presenter lo convierte en
    // el ViewModel. Lo guarda para que el controller lo lea después de ejecutar el caso de uso,
    // y por eso se registra como Scoped, compartido con el Interactor durante el pedido.
    public class SolicitarReservaPresenter : IOutputPort
    {
        public SolicitarReservaViewModel? ViewModel { get; private set; }

        public Task HandleAsync(OutputDataResponse input)
        {
            ViewModel = new SolicitarReservaViewModel(input);
            return Task.CompletedTask;
        }
    }
}
