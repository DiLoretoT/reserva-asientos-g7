// Matías De La Rosa
using Availability.Entities.Eventos;
using Availability.Entities.Solicitudes;
using Availability.UseCases.Eventos;
using Availability.UseCases.Interfaces;
using Availability.UseCases.Solicitudes.SolicitarReserva.Interfaces;
using Availability.UseCases.Solicitudes.SolicitarReserva.Models;

namespace Availability.UseCases.Solicitudes.SolicitarReserva
{
    // Interactor del caso de uso SolicitarReserva (paso 3 del enunciado), con la misma forma
    // que los de Eventos: recibe sus dependencias por constructor como interfaces de este
    // anillo y entrega el resultado por el puerto de salida. Qué clase concreta llega en cada
    // una lo decide Program.cs.
    //
    // La respuesta no espera a Booking. Availability registra la solicitud, la publica en el
    // broker y le contesta al usuario que la recibió; Booking la procesa después, a su ritmo
    // (comunicación asincrónica). Si Availability esperara el resultado, el usuario quedaría
    // bloqueado mientras Booking retiene el asiento y cobra con Payment, y una demora o una
    // caída de Booking o de Payment se trasladaría a Availability. El resultado se consulta
    // después, con el seguimiento (paso 7).
    //
    // Auditoría (issue #9, contracts/logs.md): operación "solicitar_reserva". Según el
    // contrato, su TraceId es el solicitudId, el mismo que viaja en el mensaje del broker. Así
    // la cadena de sucesos se sigue con un solo identificador en Availability, Booking y Payment.
    public class Interactor : IInputPort
    {
        private const string Operacion = "solicitar_reserva";

        private readonly IEventoRepository _eventoRepository;
        private readonly ISolicitudRepository _solicitudRepository;
        private readonly IPublicadorSolicitudes _publicador;
        private readonly IOutputPort _presenter;
        private readonly IAuditoria _auditoria;

        public Interactor(
            IEventoRepository eventoRepository,
            ISolicitudRepository solicitudRepository,
            IPublicadorSolicitudes publicador,
            IOutputPort presenter,
            IAuditoria auditoria)
        {
            _eventoRepository = eventoRepository;
            _solicitudRepository = solicitudRepository;
            _publicador = publicador;
            _presenter = presenter;
            _auditoria = auditoria;
        }

        public async Task HandleAsync(InputDataRequest input)
        {
            // El identificador único de la solicitud se genera al empezar, antes de verificar el
            // asiento, porque es el TraceId de toda la operación: así el inicio y el fin quedan
            // con el mismo TraceId, y también queda trazado un pedido que termina rechazado
            // (en ese caso el Id no llega a guardarse ni a publicarse).
            Guid solicitudId = Guid.NewGuid();
            _auditoria.AsociarTraza(solicitudId);
            _auditoria.Inicio(Operacion);

            // Arranca en Error: si algo lanza una excepción (por ejemplo, la base de Availability
            // caída), el finally registra el fin igual y la excepción sigue su camino.
            ResultadoAuditoria resultado = ResultadoAuditoria.Error;
            string? motivo = "excepcion";

            try
            {
                (resultado, motivo) = await SolicitarAsync(input, solicitudId);
            }
            finally
            {
                _auditoria.Fin(Operacion, resultado, motivo);
            }
        }

        // El trabajo del caso de uso. Devuelve cómo terminó, para que HandleAsync lo audite.
        private async Task<(ResultadoAuditoria Resultado, string? Motivo)> SolicitarAsync(InputDataRequest input, Guid solicitudId)
        {
            // 1. El asiento tiene que existir en el evento (paso 3: "verifica que el asiento
            // exista"). Si el evento no existe, evento es null y asiento también.
            Evento? evento = await _eventoRepository.ObtenerConAsientosAsync(input.EventoId);
            Asiento? asiento = evento?.Asientos.FirstOrDefault(a => a.Id == input.AsientoId);

            if (asiento is null)
            {
                await _presenter.HandleAsync(new OutputDataResponse
                {
                    Resultado = ResultadoSolicitud.AsientoInexistente,
                    EventoId = input.EventoId
                });
                return (ResultadoAuditoria.Rechazada, "asiento_inexistente");
            }

            // No se verifica si el asiento está libre. Availability no es el dueño de las
            // reservas: el único que sabe con certeza si un asiento está ocupado es Booking, en
            // el momento de procesar el mensaje. Cualquier consulta previa podría quedar vieja
            // enseguida, porque muchas personas pueden pedir el mismo asiento a la vez. Si el
            // asiento ya estaba tomado, Booking rechaza la solicitud con el motivo
            // asiento_ocupado y el usuario lo ve en el seguimiento.

            // 2. La solicitud con su identificador único. El Guid lo genera Availability y no
            // la base, porque viaja en el mensaje del broker y con él se le consulta después
            // el estado a Booking. Es el que se generó al empezar la operación (el TraceId).
            var solicitud = new SolicitudReserva
            {
                Id = solicitudId,
                EventoId = input.EventoId,
                AsientoId = input.AsientoId,
                UsuarioId = input.UsuarioId,
                FechaSolicitud = DateTime.UtcNow
            };

            // 3. Se guarda antes de publicar. Si se publicara primero y la base fallara,
            // Booking procesaría una solicitud que Availability no tiene registrada, y el
            // usuario nunca podría consultar su resultado. Guardando primero, toda solicitud
            // que llega a Booking existe en Availability, y si Booking todavía no la procesó,
            // el seguimiento la muestra "en proceso" (consistencia eventual).
            //
            // Si la base de Availability falla, la excepción se propaga: es la base propia del
            // servicio y sin ella el sitio no puede funcionar, a diferencia del broker o de
            // Booking, cuya falta sí se contempla.
            await _solicitudRepository.AgregarAsync(solicitud);

            // 4. Se publica en el broker.
            bool publicada = await _publicador.PublicarAsync(solicitud);

            if (!publicada)
            {
                // Acción compensatoria: la solicitud se guardó pero Booking nunca la va a
                // recibir, así que se borra para no dejarla "en proceso" para siempre. El
                // usuario recibe el aviso y puede volver a intentarlo.
                //
                // Una alternativa más robusta es el patrón Outbox: guardar el mensaje como
                // pendiente en la misma transacción y que un proceso en segundo plano lo
                // reintente hasta publicarlo. Para el alcance del TP alcanza con compensar.
                await _solicitudRepository.EliminarAsync(solicitud.Id);

                await _presenter.HandleAsync(new OutputDataResponse
                {
                    Resultado = ResultadoSolicitud.NoSePudoEnviar,
                    EventoId = input.EventoId,
                    Fila = asiento.Fila,
                    Numero = asiento.Numero
                });
                return (ResultadoAuditoria.Error, "broker_no_disponible");
            }

            // 5. Se entrega el resultado sin esperar a Booking.
            await _presenter.HandleAsync(new OutputDataResponse
            {
                Resultado = ResultadoSolicitud.Recibida,
                EventoId = input.EventoId,
                SolicitudId = solicitud.Id,
                Fila = asiento.Fila,
                Numero = asiento.Numero
            });
            return (ResultadoAuditoria.Ok, null);
        }
    }
}
