// Tomás Di Loreto
using Availability.Entities.Eventos;
using Availability.Entities.Solicitudes;
using Availability.UseCases.Eventos;
using Availability.UseCases.Interfaces;
using Availability.UseCases.Solicitudes.ConsultarEstadoSolicitud.Interfaces;
using Availability.UseCases.Solicitudes.ConsultarEstadoSolicitud.Models;

namespace Availability.UseCases.Solicitudes.ConsultarEstadoSolicitud
{
    // Interactor del caso de uso ConsultarEstadoSolicitud. Combina la solicitud 
    // registrada en la base de Availability, que dice qué se pidió y de quién es,
    //  y el estado que informa Booking, el dueño de las reservas.
    //
    // Auditoría (issue #9, contracts/logs.md, Matías De La Rosa): operación
    // "consultar_solicitud". Su TraceId es el solicitudId consultado: el cliente de Booking lo
    // manda en el header X-Trace-Id, así la consulta queda unida en los logs con todo lo que
    // pasó con esa solicitud en Availability, Booking y Payment.
    public class Interactor : IInputPort
    {
        private const string Operacion = "consultar_solicitud";

        private readonly ISolicitudRepository _solicitudRepository;
        private readonly IEventoRepository _eventoRepository;
        private readonly IConsultaEstadoSolicitud _consultaEstado;
        private readonly IOutputPort _presenter;
        private readonly IAuditoria _auditoria;

        public Interactor(
            ISolicitudRepository solicitudRepository,
            IEventoRepository eventoRepository,
            IConsultaEstadoSolicitud consultaEstado,
            IOutputPort presenter,
            IAuditoria auditoria)
        {
            _solicitudRepository = solicitudRepository;
            _eventoRepository = eventoRepository;
            _consultaEstado = consultaEstado;
            _presenter = presenter;
            _auditoria = auditoria;
        }

        public async Task HandleAsync(InputDataRequest input)
        {
            _auditoria.AsociarTraza(input.SolicitudId);
            _auditoria.Inicio(Operacion);

            // Arranca en Error: si algo lanza una excepción (por ejemplo, la base inconsistente),
            // el finally registra el fin igual y la excepción sigue su camino.
            ResultadoAuditoria resultadoAuditoria = ResultadoAuditoria.Error;
            string? motivo = "excepcion";

            try
            {
                (resultadoAuditoria, motivo) = await ConsultarAsync(input);
            }
            finally
            {
                _auditoria.Fin(Operacion, resultadoAuditoria, motivo);
            }
        }

        // El trabajo del caso de uso. Devuelve cómo terminó, para que HandleAsync lo audite.
        private async Task<(ResultadoAuditoria Resultado, string? Motivo)> ConsultarAsync(InputDataRequest input)
        {
            SolicitudReserva? solicitud = await _solicitudRepository.ObtenerAsync(input.SolicitudId);

            // Cada usuario ve solo sus solicitudes. La regla va en el caso de uso porque es
            // una regla del sistema. Una solicitud ajena se informa como inexistente y no se
            // le pregunta a Booking.
            if (solicitud is null || solicitud.UsuarioId != input.UsuarioId)
            {
                await _presenter.HandleAsync(new OutputDataResponse
                {
                    SolicitudEncontrada = false,
                    SolicitudId = input.SolicitudId
                });

                // En la auditoría también se registran igual la inexistente y la ajena, con el
                // mismo criterio que la pantalla.
                return (ResultadoAuditoria.Rechazada, "solicitud_inexistente");
            }

            // El evento y el asiento salen de la base de Availability. La solicitud los referencia
            // con claves foráneas, así que siempre existen. Si faltan, la base está inconsistente 
            // y se lanza una excepción en vez de mostrar datos vacíos.
            Evento evento = await _eventoRepository.ObtenerConAsientosAsync(solicitud.EventoId)
                ?? throw new InvalidOperationException($"La solicitud {solicitud.Id} referencia un evento inexistente.");
            Asiento asiento = evento.Asientos.FirstOrDefault(a => a.Id == solicitud.AsientoId)
                ?? throw new InvalidOperationException($"La solicitud {solicitud.Id} referencia un asiento inexistente.");

            ResultadoEstadoSolicitud resultado = await _consultaEstado.ObtenerEstadoAsync(solicitud.Id);

            await _presenter.HandleAsync(new OutputDataResponse
            {
                SolicitudEncontrada = true,
                SolicitudId = solicitud.Id,
                FechaSolicitud = solicitud.FechaSolicitud,
                NombreEvento = evento.Nombre,
                FechaEvento = evento.Fecha,
                Fila = asiento.Fila,
                NumeroAsiento = asiento.Numero,
                Estado = CalcularEstado(resultado),
                Motivo = resultado.RespuestaOk && resultado.Procesada ? resultado.Motivo : null
            });

            // Cualquier estado que informe Booking, incluido "en proceso" (404), es un resultado
            // Ok de la consulta. Es Error solo si Booking no respondió.
            return resultado.RespuestaOk
                ? (ResultadoAuditoria.Ok, null)
                : (ResultadoAuditoria.Error, "booking_no_disponible");
        }

        // Traduce la respuesta de Booking al estado de la pantalla. El 404 de Booking no 
        // es un error, sino que significa "en proceso".Availability sabe que la solicitud 
        // existe porque la encontró en su base, y Booking todavía no la procesó 
        // (consistencia eventual).
        private static EstadoSolicitud CalcularEstado(ResultadoEstadoSolicitud resultado)
        {
            if (!resultado.RespuestaOk)
            {
                return EstadoSolicitud.Desconocido;
            }

            if (!resultado.Procesada)
            {
                return EstadoSolicitud.EnProceso;
            }

            return resultado.Estado switch
            {
                EstadoReserva.Retenida => EstadoSolicitud.Retenida,
                EstadoReserva.Confirmada => EstadoSolicitud.Confirmada,
                EstadoReserva.Rechazada => EstadoSolicitud.Rechazada,
                EstadoReserva.Vencida => EstadoSolicitud.Vencida,
                _ => throw new ArgumentOutOfRangeException(nameof(resultado), resultado.Estado, "Estado de reserva no contemplado.")
            };
        }
    }
}