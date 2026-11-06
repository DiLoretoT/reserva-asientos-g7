using Availability.UseCases.Eventos.ListarEventos.Models;
using Availability.UseCases.Interfaces;

namespace Availability.UseCases.Eventos.ListarEventos.Interfaces
{
    // Puerto de salida del caso de uso. Lo implementa el presenter en Availability.Presenters,
    // así el Interactor entrega la lista sin saber cómo se va a mostrar.
    public interface IOutputPort : IPort<OutputDataResponse>
    {
    }
}
