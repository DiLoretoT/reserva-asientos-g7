// Matías De La Rosa
using Availability.UseCases.Interfaces;
using Availability.UseCases.Solicitudes.SolicitarReserva.Models;

namespace Availability.UseCases.Solicitudes.SolicitarReserva.Interfaces
{
    // Puerto de salida del caso de uso. Lo define este anillo y lo implementa el presenter en
    // Availability.Presenters: el Interactor entrega el resultado y el presenter decide cómo
    // se muestra.
    public interface IOutputPort : IPort<OutputDataResponse>
    {
    }
}
