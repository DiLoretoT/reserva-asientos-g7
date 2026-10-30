namespace Booking.Domain;

public class Reserva
{
    public Guid Id { get; set; }
    public Guid SolicitudId { get; set; }
    public Guid EventoId { get; set; }
    public Guid AsientoId { get; set; }
    public required string UsuarioId { get; set; }
    public required EstadoReserva Estado { get; set; }
    public MotivoRechazo? Motivo { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaVencimiento { get; set; }
    public DateTime FechaActualizacion { get; set; }
}
