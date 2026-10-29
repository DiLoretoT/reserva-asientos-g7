// Tomás Di Loreto
using Availability.Gateways.Persistencia;
using Availability.UseCases.Interfaces;
using Microsoft.Extensions.Logging;

namespace Availability.Gateways.Booking
{
    // Simulación de la consulta de ocupación a Booking, mientras Booking no existe.
    // Implementa IConsultaOcupacion igual que lo va a hacer el cliente HTTP real, así que
    // reemplazarla es cambiar el registro en DependencyInjection.cs.
    // Arma la misma respuesta que el endpoint 1 del contrato (AsientosOcupadosResponse) y la
    // traduce con MapeosBooking, el mismo traductor que va a usar el cliente real. Así la
    // traducción del contrato queda probada desde ahora.
    public class ConsultaOcupacionSimulada : IConsultaOcupacion
    {
        private readonly bool _simularSinRespuesta;
        private readonly ILogger<ConsultaOcupacionSimulada> _logger;

        // simularSinRespuesta = true hace de cuenta que Booking está caído. El valor sale de
        // appsettings.Development.json (Booking:SimularSinRespuesta), así se prueba el aviso
        // de ocupación no disponible sin tocar el código.
        public ConsultaOcupacionSimulada(bool simularSinRespuesta, ILogger<ConsultaOcupacionSimulada> logger)
        {
            _simularSinRespuesta = simularSinRespuesta;
            _logger = logger;
        }

        public Task<ResultadoOcupacion> ObtenerAsientosOcupadosAsync(Guid eventoId)
        {
            try
            {
                // Se lanza la misma excepción que HttpClient cuando no logra conectarse con
                // Booking, para que la falla simulada recorra el mismo camino que una falla real.
                if (_simularSinRespuesta)
                {
                    throw new HttpRequestException("Simulación: Booking no responde.");
                }

                AsientosOcupadosResponse respuesta = ArmarRespuesta(eventoId);
                return Task.FromResult(respuesta.AResultado());
            }
            catch (Exception ex) when (ex is HttpRequestException or FormatException)
            {
                // Cumple lo que promete IConsultaOcupacion, que implica que si no hay una 
                // respuesta válida, no propaga la excepción y devuelve RespuestaOk = false. 
                _logger.LogWarning(ex, "No se pudo consultar la ocupación del evento {EventoId} a Booking.", eventoId);
                return Task.FromResult(new ResultadoOcupacion { RespuestaOk = false });
            }
        }

        // Solo el evento 1 tiene asientos ocupados, con A2 y C1 retenidas y A3 y B4
        // confirmadas. Para cualquier otro evento devuelve la lista vacía, igual que Booking,
        // que según el contrato no distingue un evento inexistente de uno sin reservas.
        // Los Guid salen de DatosIniciales para que los asientos ocupados existan en la base.
        // En el sistema real, Booking los conoce porque Availability los manda en cada
        // solicitud de reserva.
        private static AsientosOcupadosResponse ArmarRespuesta(Guid eventoId)
        {
            var respuesta = new AsientosOcupadosResponse { EventoId = eventoId };

            if (eventoId != DatosIniciales.IdEvento(1))
            {
                return respuesta;
            }

            respuesta.AsientosOcupados.Add(Ocupado(fila: 1, numero: 2, "retenida"));   // A2
            respuesta.AsientosOcupados.Add(Ocupado(fila: 3, numero: 1, "retenida"));   // C1
            respuesta.AsientosOcupados.Add(Ocupado(fila: 1, numero: 3, "confirmada")); // A3
            respuesta.AsientosOcupados.Add(Ocupado(fila: 2, numero: 4, "confirmada")); // B4

            return respuesta;
        }

        private static AsientoOcupadoResponse Ocupado(int fila, int numero, string estado) =>
            new()
            {
                AsientoId = DatosIniciales.IdAsiento(1, fila, numero),
                Estado = estado
            };
    }
}