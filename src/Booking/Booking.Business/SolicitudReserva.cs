namespace Booking.Business;

// Los datos de la solicitud que Availability publica en el broker y Booking recibe en el paso 4. Es un record
// porque son datos que no cambian, y es de Business y no la clase del mensaje del broker para que esta capa no
// dependa de cómo se lea el mensaje.
// El evento y el asiento van solo por Id porque Booking no conoce esos datos (son de Availability), y el
// usuario es string porque su formato lo define el servidor de autorización.
public record SolicitudReserva(Guid SolicitudId, Guid EventoId, Guid AsientoId, string UsuarioId);
