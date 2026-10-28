// Tomás Di Loreto
namespace Availability.UseCases.Interfaces
{
    // Puerto para consultar qué asientos están ocupados. Lo implementa una clase 
    // del anillo esterior, que finalmente llamará al endpoint de booking.

    public interface IConsultaOcupacion
    {
        // Devuelve los asientos ocupados del evento con el estado de cada uno.

        // Si Booking no responde (servicio caído, tiempo de espera agotado o respuesta con
        // error), la implementación devuelve un resultado con RespuestaOk = false. Que 
        // Booking no responda es una situación esperada en un sistema distribuido, y así la 
        // firma obliga al Interactor a contemplarla.

        Task<ResultadoOcupacion> ObtenerAsientosOcupadosAsync(Guid eventoId);
    }

    public class ResultadoOcupacion
    {
        // true si la consulta obtuvo respuesta. Si es false, AsientosOcupados viene vacía y
        // no significa que el evento esté libre, sino que no se sabe.
        public bool RespuestaOk { get; set; }

        // Solo los asientos ocupados. Booking no sabe qué asientos existen. Los libres
        // los calcula el Interactor comparando con los asientos del evento.
        public List<AsientoOcupado> AsientosOcupados { get; set; } = new();
    }
    // Un asiento ocupado tal como lo informa el endpoint 1 del contrato.
    public class AsientoOcupado
    {
        public Guid AsientoId { get; set; }

        public EstadoOcupacion Estado { get; set; }
    }

    // Los dos estados que el contrato permite para un asiento ocupado. Es distinto de
    // EstadoAsiento del OutputDataResponse, ya que este enum refleja lo que dice Booking, 
    // y el Interactor lo traduce a lo que muestra la página (Retenida a Pendiente,
    // Confirmada a Reservado).
    public enum EstadoOcupacion
    {
        Retenida,
        Confirmada
    }
}