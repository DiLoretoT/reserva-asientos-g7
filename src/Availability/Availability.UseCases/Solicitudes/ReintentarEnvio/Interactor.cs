using Availability.Entities.Solicitudes;
using Availability.UseCases.Interfaces;
using Availability.UseCases.Solicitudes.ReintentarEnvio.Interfaces;
using Availability.UseCases.Solicitudes.ReintentarEnvio.Models;

namespace Availability.UseCases.Solicitudes.ReintentarEnvio
{
    // Interactor del caso de uso ReintentarEnvio: la parte del patrón Outbox que reenvía.
    // SolicitarReserva guarda siempre la solicitud en la base de Availability y, si el broker
    // no responde, la deja pendiente de envío. Este caso de uso busca esas solicitudes y las
    // vuelve a publicar hasta que el broker confirma. Así una caída del broker demora el envío
    // pero no pierde ninguna solicitud.
    //
    // Es un caso de uso propio y no un método del servicio en segundo plano porque decidir qué
    // se reenvía y cuándo una solicitud queda enviada es una regla del sistema. El servicio en
    // segundo plano de Availability.Web solo decide cada cuánto se ejecuta, igual que un
    // controller decide a qué dirección responde.
    //
    // No tiene puerto de salida ni presenter porque no hay pantalla: nadie espera el
    // resultado. Lo que pasó con cada solicitud queda en la auditoría.
    //
    // Cada reenvío usa el mismo solicitudId de la solicitud original, que es el MessageId del
    // contrato del broker. Si el primer intento sí había llegado al broker (un timeout no lo
    // descarta), Booking reconoce el mensaje repetido y no procesa dos veces la misma
    // solicitud.
    //
    // Auditoría (contracts/logs.md): operación "reintentar_envio", una por cada solicitud, con
    // el solicitudId como TraceId. Así el reenvío queda en la misma cadena de sucesos que el
    // solicitar_reserva que la creó y que lo que después hacen Booking y Payment.
    public class Interactor : IInputPort
    {
        private const string Operacion = "reintentar_envio";

        // Solo se reenvían solicitudes registradas hace más de este tiempo. SolicitarReserva
        // puede estar publicando todavía una solicitud recién creada, y reenviarla en ese
        // momento produciría un mensaje repetido. El valor supera el tiempo de espera por
        // defecto del cliente de Azure Service Bus (60 segundos), así una publicación lenta de
        // SolicitarReserva termina antes de que este caso de uso la tome.
        private static readonly TimeSpan AntiguedadMinima = TimeSpan.FromSeconds(90);

        // Cuántas pendientes se leen por vuelta. Con el broker caído durante horas pueden
        // acumularse muchas, y no tiene sentido cargarlas todas si la vuelta se corta en la
        // primera que no sale. Las que quedan afuera se toman en las vueltas siguientes.
        private const int MaximoPorVuelta = 50;

        private readonly ISolicitudRepository _solicitudRepository;
        private readonly IPublicadorSolicitudes _publicador;
        private readonly IAuditoria _auditoria;

        public Interactor(
            ISolicitudRepository solicitudRepository,
            IPublicadorSolicitudes publicador,
            IAuditoria auditoria)
        {
            _solicitudRepository = solicitudRepository;
            _publicador = publicador;
            _auditoria = auditoria;
        }

        public async Task HandleAsync(InputDataRequest input)
        {
            List<SolicitudReserva> pendientes =
                await _solicitudRepository.ListarPendientesDeEnvioAsync(DateTime.UtcNow - AntiguedadMinima, MaximoPorVuelta);

            // De la más vieja a la más nueva, para que se envíen en el orden en que se pidieron.
            foreach (SolicitudReserva solicitud in pendientes)
            {
                bool publicada;
                try
                {
                    publicada = await ReintentarAsync(solicitud);
                }
                catch (Exception)
                {
                    // Una excepción con una solicitud (por ejemplo, un mensaje que el broker
                    // rechaza por su contenido) no puede frenar a las que vienen detrás: si se
                    // cortara acá, la próxima vuelta empezaría otra vez por la misma y las demás
                    // no saldrían nunca. La auditoría de esa solicitud ya quedó con error y
                    // motivo excepcion, y se vuelve a intentar en la próxima vuelta.
                    continue;
                }

                // Si el broker no responde, las demás también van a fallar. Se corta acá y se
                // espera a la próxima vuelta, en lugar de sumar un intento fallido por cada
                // solicitud pendiente.
                if (!publicada)
                {
                    break;
                }
            }
        }

        // Reenvía una solicitud y audita el intento. Devuelve si el broker la confirmó.
        private async Task<bool> ReintentarAsync(SolicitudReserva solicitud)
        {
            _auditoria.AsociarTraza(solicitud.Id);
            _auditoria.Inicio(Operacion);

            // Igual que en SolicitarReserva: si algo lanza una excepción, el finally registra el
            // fin con error y la excepción se propaga a HandleAsync.
            ResultadoAuditoria resultado = ResultadoAuditoria.Error;
            string? motivo = "excepcion";

            try
            {
                bool publicada = await _publicador.PublicarAsync(solicitud);

                if (!publicada)
                {
                    (resultado, motivo) = (ResultadoAuditoria.Error, "broker_no_disponible");
                    return false;
                }

                // Mismo criterio que SolicitarReserva: si marcarla falla, queda pendiente y se
                // vuelve a publicar con el mismo MessageId en la próxima vuelta.
                await _solicitudRepository.MarcarEnviadaAsync(solicitud.Id);

                (resultado, motivo) = (ResultadoAuditoria.Ok, null);
                return true;
            }
            finally
            {
                _auditoria.Fin(Operacion, resultado, motivo);
            }
        }
    }
}
