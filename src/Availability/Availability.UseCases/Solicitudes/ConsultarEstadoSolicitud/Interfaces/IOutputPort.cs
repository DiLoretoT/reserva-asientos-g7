// Tomás Di Loreto
using Availability.UseCases.Interfaces;
using Availability.UseCases.Solicitudes.ConsultarEstadoSolicitud.Models;

namespace Availability.UseCases.Solicitudes.ConsultarEstadoSolicitud.Interfaces
{
    // Puerto de salida del caso de uso. Lo define este anillo y lo implementa el presenter en
    // Availability.Presenters, así el Interactor entrega el resultado sin saber cómo se muestra.
    public interface IOutputPort : IPort<OutputDataResponse>
    {
    }
}