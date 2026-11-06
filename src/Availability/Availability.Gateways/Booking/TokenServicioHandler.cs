// Tomás Di Loreto
using System.Net.Http.Headers;

namespace Availability.Gateways.Booking
{
    // Agrega el header "Authorization: Bearer <token>" a cada llamada a Booking. Es un
    // DelegatingHandler, una pieza que se engancha en el HttpClient y procesa cada pedido
    // antes de que salga. Así ClienteBooking no se ocupa del token, y lo lleva cualquier
    // llamada nueva a Booking.
    public class TokenServicioHandler : DelegatingHandler
    {
        private readonly TokenServicio _tokenServicio;

        public TokenServicioHandler(TokenServicio tokenServicio)
        {
            _tokenServicio = tokenServicio;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            string token = await _tokenServicio.ObtenerAsync(cancellationToken);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            return await base.SendAsync(request, cancellationToken);
        }
    }
}