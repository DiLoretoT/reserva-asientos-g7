using Booking.Domain;

namespace Booking.Data;

// Dapper lee texto en este registro; la conversión explícita evita depender de su mapeo de enums.
public sealed class ReservaRegistro
{
    private static readonly IReadOnlyDictionary<EstadoReserva, string> Estados =
        new Dictionary<EstadoReserva, string>
        {
            [EstadoReserva.Retenida] = "retenida",
            [EstadoReserva.Confirmada] = "confirmada",
            [EstadoReserva.Rechazada] = "rechazada",
            [EstadoReserva.Vencida] = "vencida"
        };

    private static readonly IReadOnlyDictionary<MotivoRechazo, string> Motivos =
        new Dictionary<MotivoRechazo, string>
        {
            [MotivoRechazo.AsientoOcupado] = "asiento_ocupado",
            [MotivoRechazo.PagoFallido] = "pago_fallido",
            [MotivoRechazo.PagoNoDisponible] = "pago_no_disponible"
        };

    public const string ColumnasSql = """
        id AS Id, solicitud_id AS SolicitudId, evento_id AS EventoId, asiento_id AS AsientoId,
        usuario_id AS UsuarioId, estado AS Estado, motivo_rechazo AS Motivo,
        fecha_creacion AS FechaCreacion, fecha_vencimiento AS FechaVencimiento,
        fecha_actualizacion AS FechaActualizacion
        """;

    public Guid Id { get; set; }
    public Guid SolicitudId { get; set; }
    public Guid EventoId { get; set; }
    public Guid AsientoId { get; set; }
    public string UsuarioId { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public string? Motivo { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaVencimiento { get; set; }
    public DateTime FechaActualizacion { get; set; }

    public static ReservaRegistro DesdeDominio(Reserva reserva) => new()
    {
        Id = reserva.Id,
        SolicitudId = reserva.SolicitudId,
        EventoId = reserva.EventoId,
        AsientoId = reserva.AsientoId,
        UsuarioId = reserva.UsuarioId,
        Estado = ATexto(reserva.Estado, Estados),
        Motivo = reserva.Motivo is { } motivo ? ATexto(motivo, Motivos) : null,
        FechaCreacion = reserva.FechaCreacion,
        FechaVencimiento = reserva.FechaVencimiento,
        FechaActualizacion = reserva.FechaActualizacion
    };

    public Reserva ADominio() => new()
    {
        Id = Id,
        SolicitudId = SolicitudId,
        EventoId = EventoId,
        AsientoId = AsientoId,
        UsuarioId = UsuarioId,
        Estado = DesdeTexto(Estado, Estados),
        Motivo = Motivo is { } motivo ? DesdeTexto(motivo, Motivos) : null,
        FechaCreacion = FechaCreacion,
        FechaVencimiento = FechaVencimiento,
        FechaActualizacion = FechaActualizacion
    };

    private static string ATexto<T>(T valor, IReadOnlyDictionary<T, string> valores) where T : struct, Enum
        => valores.TryGetValue(valor, out var texto)
            ? texto
            : throw new ArgumentOutOfRangeException(nameof(valor), valor, "Valor de reserva desconocido.");

    private static T DesdeTexto<T>(string texto, IReadOnlyDictionary<T, string> valores) where T : struct, Enum
    {
        foreach (var par in valores)
        {
            if (par.Value == texto)
                return par.Key;
        }

        throw new FormatException($"Valor de reserva desconocido: '{texto}'.");
    }
}
