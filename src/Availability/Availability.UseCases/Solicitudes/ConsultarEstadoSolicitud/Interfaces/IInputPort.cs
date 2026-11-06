// Tomás Di Loreto
using Availability.UseCases.Interfaces;
using Availability.UseCases.Solicitudes.ConsultarEstadoSolicitud.Models;

namespace Availability.UseCases.Solicitudes.ConsultarEstadoSolicitud.Interfaces
{
    // Puerto de entrada del caso de uso. Lo implementa el Interactor y lo llama el controller,
    // que depende de esta interfaz y no de la clase Interactor.
    public interface IInputPort : IPort<InputDataRequest>
    {
    }
}