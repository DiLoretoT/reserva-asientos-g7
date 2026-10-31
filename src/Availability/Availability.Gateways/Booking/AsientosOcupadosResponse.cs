// Tomás Di Loreto
namespace Availability.Gateways.Booking
{
    // Respuesta del endpoint 1 de contracts/availability-booking.md, con los mismos campos
    // que el JSON. Es la forma del dato del lado de Booking
    public class AsientosOcupadosResponse
    {
        public Guid EventoId { get; set; }

        public List<AsientoOcupadoResponse> AsientosOcupados { get; set; } = new();
    }
    public class AsientoOcupadoResponse
    {
        public Guid AsientoId { get; set; }
        // Texto tal cual viaja en el contrato, como "retenida" o "confirmada". 
        // Lo traduce el mapper.
        public string Estado { get; set; } = string.Empty;
    }
}