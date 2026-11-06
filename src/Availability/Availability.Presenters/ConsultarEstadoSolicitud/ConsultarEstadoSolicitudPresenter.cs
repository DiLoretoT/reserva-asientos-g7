// Tomás Di Loreto
using Availability.UseCases.Solicitudes.ConsultarEstadoSolicitud.Interfaces;
using Availability.UseCases.Solicitudes.ConsultarEstadoSolicitud.Models;

namespace Availability.Presenters.ConsultarEstadoSolicitud
{
    // Implementa el puerto de salida del caso de uso y convierte el OutputDataResponse en el
    // ViewModel de la página. Guarda el ViewModel para que el controller lo lea después de
    // ejecutar el caso de uso, por eso es Scoped, igual que ListarAsientosPresenter.
    public class ConsultarEstadoSolicitudPresenter : IOutputPort
    {
        public ConsultarEstadoSolicitudViewModel? ViewModel { get; private set; }

        public Task HandleAsync(OutputDataResponse input)
        {
            ViewModel = new ConsultarEstadoSolicitudViewModel(input);
            return Task.CompletedTask;
        }
    }
}