// Tomás Di Loreto
using Availability.Presenters.ConsultarEstadoSolicitud;
using Availability.UseCases.Solicitudes.ConsultarEstadoSolicitud.Interfaces;
using Availability.UseCases.Solicitudes.ConsultarEstadoSolicitud.Models;

namespace Availability.Controllers.Solicitudes
{
    // Controller del caso de uso en el anillo 3, con la misma forma que ListarAsientosController
    // Una clase común, sin ASP.NET Core. Recibe el usuario como un string y no conoce la cookie
    // ni los claims; eso lo resuelve el controller del anillo 4.
    public class ConsultarEstadoSolicitudController
    {
        private readonly IInputPort _interactor;
        private readonly ConsultarEstadoSolicitudPresenter _presenter;

        public ConsultarEstadoSolicitudController(IInputPort interactor, ConsultarEstadoSolicitudPresenter presenter)
        {
            _interactor = interactor;
            _presenter = presenter;
        }

        public async Task<ConsultarEstadoSolicitudViewModel> ConsultarEstado(Guid solicitudId, string usuarioId)
        {
            await _interactor.HandleAsync(new InputDataRequest
            {
                SolicitudId = solicitudId,
                UsuarioId = usuarioId
            });

            return _presenter.ViewModel
                ?? throw new InvalidOperationException("El caso de uso no entregó un resultado al presenter.");
        }
    }
}