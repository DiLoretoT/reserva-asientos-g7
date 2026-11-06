// Tomás Di Loreto
namespace Availability.UseCases.Solicitudes.ConsultarEstadoSolicitud.Models
{
    // Datos de entrada del caso de uso. Incluye qué solicitud se consulta y quién la consulta.
    public class InputDataRequest
    {
        public Guid SolicitudId { get; set; }

        // El sub del token de Keycloak del usuario logueado. Llega como dato y no se lee de la
        // cookie acá, así el caso de uso no depende de ASP.NET Core ni de cómo se autentica el
        // usuario. Lo saca de la sesión el controller del anillo 4.
        public string UsuarioId { get; set; } = string.Empty;
    }
}