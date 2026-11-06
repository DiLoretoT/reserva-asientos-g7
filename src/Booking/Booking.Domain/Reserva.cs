namespace Booking.Domain;

// Entidad de dominio: la reserva de un asiento. Es anémica: solo tiene datos y las reglas están en las clases
// de servicio de Booking.Business. Es una sola tabla porque cada solicitud genera una reserva que va cambiando
// de estado (retenida, confirmada, rechazada o vencida). El mapeo a la base está en BookingDbContext, así
// Domain no depende de Entity Framework.
public class Reserva
{
    // Guid generado por la aplicación, así se puede armar la reserva antes de guardarla.
    public Guid Id { get; set; }

    // Id de la solicitud que publicó Availability. Es único: una solicitud genera una sola reserva, y así no se
    // duplica si el broker entrega el mensaje dos veces. También es lo que Availability usa para consultar el estado.
    public Guid SolicitudId { get; set; }

    // Evento y asiento van solo por Id: Booking no conoce los eventos ni los asientos, son de Availability y
    // cada servicio tiene su base. Por eso tampoco hay clave foránea.
    public Guid EventoId { get; set; }

    public Guid AsientoId { get; set; }

    // Quién reservó. Es string porque el formato lo define el servidor de autorización.
    public required string UsuarioId { get; set; }

    // Obligatorio para no asumir que toda solicitud se retiene: puede nacer retenida o rechazada.
    public required EstadoReserva Estado { get; set; }

    // Por qué se rechazó. Solo tiene valor si el estado es rechazada, en los demás es null.
    public MotivoRechazo? Motivo { get; set; }

    // Las completa la base con su propia hora, porque hay varias réplicas y sus relojes pueden diferir.
    public DateTime FechaCreacion { get; set; }

    // Hasta cuándo se retiene el asiento. Es null en las rechazadas, que no retienen nada.
    public DateTime? FechaVencimiento { get; set; }

    public DateTime FechaActualizacion { get; set; }
}
