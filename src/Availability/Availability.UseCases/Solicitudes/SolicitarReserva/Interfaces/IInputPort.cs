// Matías De La Rosa
using Availability.UseCases.Interfaces;
using Availability.UseCases.Solicitudes.SolicitarReserva.Models;

namespace Availability.UseCases.Solicitudes.SolicitarReserva.Interfaces
{
    // Puerto de entrada del caso de uso. Lo implementa el Interactor y lo llama el
    // controller, que depende de esta interfaz y no de la clase Interactor.
    public interface IInputPort : IPort<InputDataRequest>
    {
    }
}
