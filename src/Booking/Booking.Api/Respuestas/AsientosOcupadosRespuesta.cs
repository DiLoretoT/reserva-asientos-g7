using Booking.Business;

namespace Booking.Api.Respuestas;

// El cuerpo JSON del endpoint 1: el id del evento consultado y la lista de sus asientos ocupados.
// Vive en Api porque es parte del formato HTTP del contrato, no de la lógica de negocio.
public record AsientosOcupadosRespuesta(Guid EventoId, List<AsientoOcupado> AsientosOcupados);
