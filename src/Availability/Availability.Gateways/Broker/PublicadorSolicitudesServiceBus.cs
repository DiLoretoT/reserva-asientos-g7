// Matías De La Rosa — Facundo Phatouros
using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Availability.Entities.Solicitudes;
using Availability.UseCases.Interfaces;
using Microsoft.Extensions.Logging;

namespace Availability.Gateways.Broker
{
    // Publicador real de solicitudes de reserva sobre Azure Service Bus (paso 3 del
    // enunciado). Implementa el mismo puerto que la simulación, así que la DI elige
    // una u otra según exista la connection string y el caso de uso no cambia.
    //
    // Decisiones, cada una con su motivo:
    //
    // 1. Singleton: ServiceBusClient es thread-safe y mantiene la conexión AMQP, que
    //    es lo caro de armar. Crear un cliente por pedido abriría una conexión por
    //    envío; un solo cliente compartido sostiene todas las llamadas.
    // 2. El sender se crea una sola vez desde el cliente y se reutiliza en cada
    //    publicación, porque también es thread-safe. Los recursos se liberan al
    //    cerrar la aplicación: la DI descarta los servicios Singleton registrados y
    //    llama a DisposeAsync.
    // 3. MessageId y CorrelationId valen el solicitudId: si el mismo mensaje se
    //    vuelve a enviar, llega con el mismo MessageId y la idempotencia de Booking
    //    (la clave única sobre solicitudId) descarta el duplicado. ContentType y
    //    Subject salen del contrato del mensaje (contracts/broker.md, PR #85).
    // 4. Reintentos: tres intentos con espera corta ante fallas transitorias. Si el
    //    envío anterior realmente llegó, el reintento puede generar un duplicado;
    //    la detección de duplicados del broker está desactivada y lo descarta la
    //    clave única de Booking.
    // 5. false no significa "el mensaje no llegó": un timeout puede dejar el envío
    //    incierto (el mensaje pudo haber llegado). Qué hace Availability con una
    //    solicitud de envío incierto lo definen las parejas en el issue #85; hasta
    //    tanto, el Interactor mantiene su flujo actual y esta clase no lo cambia.
    public class PublicadorSolicitudesServiceBus : IPublicadorSolicitudes, IAsyncDisposable
    {
        // El publicador comparte la misma serialización que el simulador: nombres
        // camelCase en el JSON del cuerpo, como pide el contrato.
        private static readonly JsonSerializerOptions OpcionesJson = new(JsonSerializerDefaults.Web);

        // Nombre del tópico cuando la configuración no trae uno: sale de
        // contracts/broker-config.md (PR #86).
        public const string NombreTopicoPorDefecto = "grupo7-booking-topic";

        private readonly ServiceBusSender _sender;
        private readonly ILogger<PublicadorSolicitudesServiceBus> _logger;
        private bool _disposed;

        // Recibe el cliente (Singleton de la DI) y crea su sender una sola vez.
        public PublicadorSolicitudesServiceBus(ServiceBusClient client, string nombreTopico, ILogger<PublicadorSolicitudesServiceBus> logger)
        {
            _sender = client.CreateSender(string.IsNullOrWhiteSpace(nombreTopico) ? NombreTopicoPorDefecto : nombreTopico);
            _logger = logger;
        }

        public async Task<bool> PublicarAsync(SolicitudReserva solicitud)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            var mensaje = new ServiceBusMessage(JsonSerializer.Serialize(solicitud.AMensaje(), OpcionesJson))
            {
                MessageId = solicitud.Id.ToString(),
                CorrelationId = solicitud.Id.ToString(),
                ContentType = "application/json",
                Subject = "solicitud-reserva"
            };

            for (int intento = 1; intento <= 3; intento++)
            {
                try
                {
                    await _sender.SendMessageAsync(mensaje);
                    return true;
                }
                catch (Exception ex)
                {
                    // Los primeros dos intentos fallidos esperan un rato corto y
                    // vuelven a intentar con el mismo mensaje (mismo MessageId). En
                    // el tercer intento, sin espera: se informa false y listo.
                    if (intento < 3)
                    {
                        var espera = TimeSpan.FromSeconds(intento);
                        _logger.LogWarning(ex, "Intento {Intento}/3 de publicación de la solicitud {SolicitudId} falló; se reintenta en {Segundos} s.",
                            intento, solicitud.Id, espera.TotalSeconds);
                        await Task.Delay(espera);
                        continue;
                    }

                    // Sin más reintentos: false puede ser un envío incierto, como explica
                    // el encabezado de la clase (punto 5). El Interactor decide el destino.
                    _logger.LogError(ex, "La solicitud {SolicitudId} no se pudo publicar tras 3 intentos.", solicitud.Id);
                    return false;
                }
            }

            // Inalcanzable por el bucle, compilador exige un retorno.
            return false;
        }

        // El contenedor la llama al cerrar la aplicación para liberar el sender.
        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            _disposed = true;
            await _sender.DisposeAsync();
            GC.SuppressFinalize(this);
        }
    }
}
