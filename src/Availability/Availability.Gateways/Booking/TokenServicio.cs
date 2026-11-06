// Tomás Di Loreto
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Availability.Gateways.Booking
{
    // Pide a Keycloak el token de servicio de availability-web con client credentials y lo
    // guarda en memoria (contracts/tokens.md). Es independiente del usuario logueado, ya que
    // el token del usuario no viaja a Booking, las llamadas entre servicios usan el token del 
    // servicio que llama.
    //
    // Se registra como Singleton para que el token guardado se comparta entre todos los
    // pedidos. Si fuera Scoped, cada pedido HTTP pediría un token nuevo.
    public class TokenServicio
    {
        // Nombre del HttpClient con el que se llama a Keycloak, registrado en DependencyInjection.
        internal const string NombreCliente = "keycloak";

        // El contrato pide renovar el token 30 segundos antes de que venza, para no mandar
        // nunca uno a punto de vencer.
        private static readonly TimeSpan MargenRenovacion = TimeSpan.FromSeconds(30);

        private readonly IHttpClientFactory _fabrica;
        private readonly OpcionesBooking _opciones;

        // Varios pedidos pueden necesitar un token al mismo tiempo. El semáforo deja pasar de a
        // uno, así se pide un solo token nuevo y los demás reusan ese.
        private readonly SemaphoreSlim _semaforo = new(1, 1);

        private string? _token;
        private DateTime _venceUtc;

        public TokenServicio(IHttpClientFactory fabrica, OpcionesBooking opciones)
        {
            _fabrica = fabrica;
            _opciones = opciones;
        }

        public async Task<string> ObtenerAsync(CancellationToken cancellationToken)
        {
            if (TokenVigente())
            {
                return _token!;
            }

            await _semaforo.WaitAsync(cancellationToken);
            try
            {
                if (TokenVigente())
                {
                    return _token!;
                }

                HttpClient http = _fabrica.CreateClient(NombreCliente);
                using var respuesta = await http.PostAsync(
                    _opciones.EndpointToken,
                    new FormUrlEncodedContent(new Dictionary<string, string>
                    {
                        ["grant_type"] = "client_credentials",
                        ["client_id"] = _opciones.ClientId,
                        ["client_secret"] = _opciones.ClientSecret,
                        ["scope"] = _opciones.Scope
                    }),
                    cancellationToken);

                // Si Keycloak rechaza el pedido, lanza HttpRequestException. El cliente de Booking
                // la trata como cualquier otra falla de la llamada y la página avisa que no se pudo 
                // consultar.
                respuesta.EnsureSuccessStatusCode();

                var cuerpo = await respuesta.Content.ReadFromJsonAsync<RespuestaToken>(cancellationToken)
                    ?? throw new JsonException("Keycloak respondió sin cuerpo.");

                _token = cuerpo.AccessToken;
                _venceUtc = DateTime.UtcNow.AddSeconds(cuerpo.ExpiresIn);
                return _token;
            }
            finally
            {
                _semaforo.Release();
            }
        }

        private bool TokenVigente() =>
            _token is not null && DateTime.UtcNow < _venceUtc - MargenRenovacion;

        private class RespuestaToken
        {
            [JsonPropertyName("access_token")]
            public string AccessToken { get; set; } = string.Empty;

            [JsonPropertyName("expires_in")]
            public int ExpiresIn { get; set; }
        }
    }
}