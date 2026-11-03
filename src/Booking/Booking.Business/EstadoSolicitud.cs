using Booking.Domain;

namespace Booking.Business;

// Lo que devuelve el endpoint de estado de solicitud: el asiento pedido, el estado de la reserva y el motivo
// (solo si fue rechazada). Es un record chico para no exponer la entidad Reserva completa.
public record EstadoSolicitud(Guid SolicitudId, Guid AsientoId, EstadoReserva Estado, MotivoRechazo? Motivo);
