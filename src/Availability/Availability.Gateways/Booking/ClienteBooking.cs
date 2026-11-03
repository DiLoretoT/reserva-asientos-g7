// Tomás Di Loreto
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Availability.UseCases.Interfaces;
using Microsoft.Extensions.Logging;

namespace Availability.Gateways.Booking
{
    // Es el cliente HTTP de Booking. Reemplaza a ConsultaOcupacionSimulada e implementa los
    // dos puertos que el anillo 2 define hacia Booking, uno por endpoint del contrato. Una
    // sola clase para los dos porque comparten la dirección base, el token y el manejo de 
    // fallas.

    // Recibe un HttpClient ya configurado (dirección base, tiempo de espera y el handler del
    // token), según el registro de DependencyInjection.cs.
    public class ClienteBooking : IConsultaOcupacion, IConsultaEstadoSolicitud
    {
        private readonly HttpClient _http;
        private readonly ILogger<ClienteBooking> _logger;

        public ClienteBooking(HttpClient http, ILogger<ClienteBooking> logger)
        {
            _http = http;
            _logger = logger;
        }

        // Endpoint 1: asientos ocupados de un evento.
        public async Task<ResultadoOcupacion> ObtenerAsientosOcupadosAsync(Guid eventoId)
        {
            try
            {
                // GetFromJsonAsync lanza HttpRequestException si Booking responde un código
                // que no es 2xx, así un 401 o un 500 terminan en el mismo catch.
                var respuesta = await _http.GetFromJsonAsync<AsientosOcupadosResponse>(
                    $"api/eventos/{eventoId}/asientos-ocupados")
                    ?? throw new JsonException("Booking respondió sin cuerpo.");

                return respuesta.AResultado();
            }
            catch (Exception ex) when (EsFallaDeBooking(ex))
            {
                // Cumple lo que promete IConsultaOcupacion, que sin respuesta válida no propaga la
                // excepción y devuelve RespuestaOk = false.
                _logger.LogWarning(ex, "No se pudo consultar la ocupación del evento {EventoId} a Booking.", eventoId);
                return new ResultadoOcupacion { RespuestaOk = false };
            }
        }

        // Endpoint 2: estado de una solicitud.
        public async Task<ResultadoEstadoSolicitud> ObtenerEstadoAsync(Guid solicitudId)
        {
            try
            {
                using var respuesta = await _http.GetAsync($"api/solicitudes/{solicitudId}");

                // El 404 no es una falla, sino que Booking todavía no procesó la solicitud. 
                // Por eso se revisa antes de EnsureSuccessStatusCode, que lo trataría como error.
                if (respuesta.StatusCode == HttpStatusCode.NotFound)
                {
                    return new ResultadoEstadoSolicitud { RespuestaOk = true, Procesada = false };
                }

                respuesta.EnsureSuccessStatusCode();

                var cuerpo = await respuesta.Content.ReadFromJsonAsync<EstadoSolicitudResponse>()
                    ?? throw new JsonException("Booking respondió sin cuerpo.");

                return cuerpo.AResultado();
            }
            catch (Exception ex) when (EsFallaDeBooking(ex))
            {
                _logger.LogWarning(ex, "No se pudo consultar el estado de la solicitud {SolicitudId} a Booking.", solicitudId);
                return new ResultadoEstadoSolicitud { RespuestaOk = false };
            }
        }

        // Las fallas esperables de una llamada por la red, como que Booking o Keycloak estén 
        // caídos o con error (HttpRequestException), tiempo de espera agotado (TaskCanceledException),
        // JSON mal formado (JsonException) o un valor fuera del contrato (FormatException, la lanza
        // MapeosBooking). Cualquier otra excepción es un error de programación y se propaga.
        private static bool EsFallaDeBooking(Exception ex) =>
            ex is HttpRequestException or TaskCanceledException or JsonException or FormatException;
    }
}