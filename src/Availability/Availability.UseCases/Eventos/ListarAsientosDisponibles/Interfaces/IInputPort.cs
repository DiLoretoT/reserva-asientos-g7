// Tomás Di Loreto
using Availability.UseCases.Eventos.ListarAsientosDisponibles.Models;
using Availability.UseCases.Interfaces;

namespace Availability.UseCases.Eventos.ListarAsientosDisponibles.Interfaces
{
    // Puerto de entrada del caso de uso. Lo implementa el Interactor y lo llama el controller. 
    // El controller depende de esta interfaz y no de la clase Interactor. 

    public interface IInputPort: IPort<InputDataRequest>
    {
    }
}
