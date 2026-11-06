using Availability.UseCases.Eventos.ListarEventos.Models;
using Availability.UseCases.Interfaces;

namespace Availability.UseCases.Eventos.ListarEventos.Interfaces
{
    // Puerto de entrada del caso de uso. Tiene el mismo nombre que el de
    // ListarAsientosDisponibles porque cada caso de uso tiene su propio namespace, igual que
    // en el ejemplo de la cátedra. Para el compilador son dos tipos distintos.
    public interface IInputPort : IPort<InputDataRequest>
    {
    }
}
