namespace Availability.Entities.Solicitudes
{
    // Entidad de dominio: el pedido de un usuario para reservar un asiento de un evento.
    // Anémica, solo datos, igual que Evento y Asiento. Las reglas (por ejemplo, que el
    // asiento pedido exista en el evento) van en los casos de uso de Availability.UseCases.
    //
    // Availability la registra en su base antes de publicarla en el broker. Por eso, cuando
    // el usuario consulta una solicitud que Booking todavía no procesó, Availability sabe
    // que existe y la muestra "en proceso" (consistencia eventual, ver el contrato REST).
    //
    // No tiene estado (retenida, confirmada, rechazada, vencida). El dueño de las reservas
    // es Booking, así que el estado se le consulta a Booking, igual que con Asiento.
    public class SolicitudReserva
    {
        // Identificador único de la solicitud (paso 3 del enunciado). Es Guid y lo genera
        // Availability al registrarla, sin depender de la base, porque viaja en el mensaje
        // del broker y es la clave con la que después se le consulta el estado a Booking.
        public Guid Id { get; set; }

        // El evento y el asiento se referencian por Id y no con objetos Evento y Asiento.
        // Son los mismos identificadores que viajan en el mensaje del broker, y la solicitud
        // solo necesita saber qué se pidió, no cargar el evento completo con sus asientos.
        public Guid EventoId { get; set; }

        public Guid AsientoId { get; set; }

        // Identifica al usuario que hizo el pedido. Sale del token del servidor de
        // autorización; es string porque el formato del identificador lo define ese
        // servidor (contrato de tokens, issue #45).
        public string UsuarioId { get; set; } = string.Empty;

        // Cuándo se registró la solicitud, en UTC para que no dependa de la zona horaria
        // del servidor. Sirve para decidir cuánto tiempo mostrarla "en proceso".
        public DateTime FechaSolicitud { get; set; }
    }
}
