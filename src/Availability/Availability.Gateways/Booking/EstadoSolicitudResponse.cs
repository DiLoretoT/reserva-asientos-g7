// Tomás Di Loreto
namespace Availability.Gateways.Booking
{
    // Respuesta del endpoint 2 de contracts/availability-booking.md, con los mismos campos que
    // el JSON. Es la forma del dato del lado de Booking, que la traduce MapeosBooking.
    public class EstadoSolicitudResponse
    {
        public Guid SolicitudId { get; set; }

        public Guid AsientoId { get; set; }

        // Texto tal cual viaja en el contrato, como "confirmada" o "rechazada".
        public string Estado { get; set; } = string.Empty;

        // Texto como "pago_fallido", o null si la reserva no fue rechazada.
        public string? Motivo { get; set; }
    }
}