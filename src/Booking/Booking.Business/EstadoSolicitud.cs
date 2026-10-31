using Booking.Domain;

namespace Booking.Business;

// El resultado de consultar una solicitud: guarda el id de la solicitud, el asiento que pidió, el estado
// de su reserva (retenida, confirmada, rechazada o vencida) y el motivo, que solo tiene valor si fue rechazada.
// Es lo que devuelve el endpoint de estado de solicitud.
public record EstadoSolicitud(Guid SolicitudId, Guid AsientoId, EstadoReserva Estado, MotivoRechazo? Motivo);
