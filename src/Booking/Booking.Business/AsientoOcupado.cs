using Booking.Domain;

namespace Booking.Business;

// Un asiento ocupado de un evento: guarda el id del asiento y el estado de la reserva que lo ocupa
// (retenida o confirmada). Es lo que devuelve el endpoint de asientos ocupados por cada asiento.
public record AsientoOcupado(Guid AsientoId, EstadoReserva Estado);
