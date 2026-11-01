// Tomás Di Loreto
namespace Availability.Gateways.Booking
{
    // Datos para hablar con Booking y pedir el token de servicio. Los arma Program.cs desde
    // la configuración y los pasa a AddGateways, igual que el connection string, así Gateways
    // no lee la configuración por su cuenta.
    public class OpcionesBooking
    {
        // Dirección base de Booking
        public string BaseUrl { get; set; } = string.Empty;

        // Endpoint de tokens de Keycloak (contracts/tokens.md).
        public string EndpointToken { get; set; } = string.Empty;

        // Client de Availability en Keycloak (availability-web). El secret llega de User
        // Secrets y no del repo.
        public string ClientId { get; set; } = string.Empty;

        public string ClientSecret { get; set; } = string.Empty;

        // Scope que se pide junto con el token (booking-api). Keycloak agrega la audiencia
        // booking-api solo si el token se pide con este scope, sino, se rechaza con 401
        public string Scope { get; set; } = string.Empty;
    }
}