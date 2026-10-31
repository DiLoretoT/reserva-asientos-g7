// Matías De La Rosa
namespace Availability.UseCases.Solicitudes.SolicitarReserva.Models
{
    // Datos de entrada del caso de uso: qué asiento de qué evento pide quién.
    public class InputDataRequest
    {
        public Guid EventoId { get; set; }

        public Guid AsientoId { get; set; }

        // Identificador del usuario que inició sesión. El caso de uso lo recibe ya resuelto y
        // no sabe de dónde sale: lo toma Availability.Web del token de Keycloak (claim "sub"),
        // así este anillo no depende de cómo se autentica el usuario.
        public string UsuarioId { get; set; } = string.Empty;
    }
}
