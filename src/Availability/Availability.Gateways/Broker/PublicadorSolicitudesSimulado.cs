// Matías De La Rosa
using System.Text.Json;
using Availability.Entities.Solicitudes;
using Availability.UseCases.Interfaces;
using Microsoft.Extensions.Logging;

namespace Availability.Gateways.Broker
{
    // Simulación de la publicación en el broker, mientras no tengamos el connection string de
    // Azure Service Bus. Implementa IPublicadorSolicitudes igual que lo va a hacer el
    // publicador real, así que reemplazarla es cambiar el registro en DependencyInjection.cs.
    //
    // Arma el mismo mensaje que va a mandar el publicador real (SolicitudReservaMensaje, con
    // MapeosBroker) y lo escribe en el log como JSON. Así se ve exactamente qué recibiría
    // Booking, y la traducción al mensaje queda probada desde ahora.
    public class PublicadorSolicitudesSimulado : IPublicadorSolicitudes
    {
        // Las mismas opciones que usa ASP.NET Core para JSON (JsonSerializerDefaults.Web):
        // nombres en camelCase, como pide el contrato.
        private static readonly JsonSerializerOptions OpcionesJson = new(JsonSerializerDefaults.Web);

        private readonly bool _simularSinRespuesta;
        private readonly ILogger<PublicadorSolicitudesSimulado> _logger;

        // simularSinRespuesta = true hace de cuenta que el broker está caído. El valor sale de
        // appsettings.Development.json (Broker:SimularSinRespuesta), así se prueba el aviso
        // al usuario y la acción compensatoria del caso de uso sin tocar el código.
        public PublicadorSolicitudesSimulado(bool simularSinRespuesta, ILogger<PublicadorSolicitudesSimulado> logger)
        {
            _simularSinRespuesta = simularSinRespuesta;
            _logger = logger;
        }

        public Task<bool> PublicarAsync(SolicitudReserva solicitud)
        {
            try
            {
                // Se lanza una excepción como haría el cliente real cuando no logra hablar con
                // el broker, para que la falla simulada recorra el mismo camino que una real.
                // El publicador de Service Bus va a capturar ServiceBusException en este lugar.
                if (_simularSinRespuesta)
                {
                    throw new TimeoutException("Simulación: el broker no responde.");
                }

                string mensaje = JsonSerializer.Serialize(solicitud.AMensaje(), OpcionesJson);

                _logger.LogInformation(
                    "Simulación: se publicaría en el broker la solicitud {SolicitudId}: {Mensaje}",
                    solicitud.Id, mensaje);

                return Task.FromResult(true);
            }
            catch (TimeoutException ex)
            {
                // Cumple lo que promete IPublicadorSolicitudes: si el broker no responde, no
                // propaga la excepción y devuelve false para que el caso de uso lo contemple.
                _logger.LogWarning(ex, "No se pudo publicar la solicitud {SolicitudId} en el broker.", solicitud.Id);
                return Task.FromResult(false);
            }
        }
    }
}
