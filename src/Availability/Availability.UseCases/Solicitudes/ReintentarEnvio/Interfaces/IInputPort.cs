using Availability.UseCases.Interfaces;
using Availability.UseCases.Solicitudes.ReintentarEnvio.Models;

namespace Availability.UseCases.Solicitudes.ReintentarEnvio.Interfaces
{
    // Puerto de entrada del caso de uso. Lo implementa el Interactor y lo llama el servicio en
    // segundo plano de Availability.Web, igual que los controllers llaman al suyo.
    public interface IInputPort : IPort<InputDataRequest>
    {
    }
}
