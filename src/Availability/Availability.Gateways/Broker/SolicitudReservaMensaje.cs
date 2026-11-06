// Matías De La Rosa
namespace Availability.Gateways.Broker
{
    // Mensaje que Availability publica en el broker por cada solicitud de reserva (paso 3).
    // Es la forma del dato del lado del contrato con Booking, igual que
    // AsientosOcupadosResponse lo es para el endpoint REST.
    //
    // Es una propuesta para el contrato del mensaje del broker (issue #44), que todavía no
    // está definido. Sigue las convenciones de contracts/availability-booking.md: nombres en
    // camelCase en el JSON e identificadores Guid generados por Availability. Si el contrato
    // final cambia, se tocan esta clase y MapeosBroker, y el caso de uso no se entera.
    public class SolicitudReservaMensaje
    {
        // Identificador único de la solicitud. Booking lo guarda en la reserva (es su clave
        // alternativa SolicitudId) y Availability lo usa para consultar el estado (paso 7).
        // También le sirve a Booking para detectar un mensaje repetido.
        public Guid SolicitudId { get; set; }

        public Guid EventoId { get; set; }

        public Guid AsientoId { get; set; }

        // Quién hizo el pedido. Viaja en el mensaje porque, según contracts/tokens.md, el
        // token del usuario no llega a Booking: las llamadas entre servicios usan el token de
        // servicio del que llama, y por el broker no viaja ningún token del usuario. Si Booking
        // necesita saber de quién es la reserva (por ejemplo, para el servicio de
        // notificaciones del punto h), el único lugar de donde puede sacarlo es este campo.
        public string UsuarioId { get; set; } = string.Empty;

        // Cuándo se registró la solicitud, en UTC. En el JSON viaja en formato ISO 8601
        // con la Z final, por ejemplo "2026-10-31T15:30:00Z".
        public DateTime FechaSolicitud { get; set; }
    }
}
