using Booking.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Booking.Data;

public class BookingDbContext(DbContextOptions<BookingDbContext> options) : DbContext(options)
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

    public DbSet<Reserva> Reservas => Set<Reserva>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var reserva = modelBuilder.Entity<Reserva>();
        // Las reglas de integridad se declaran en el modelo y PostgreSQL las hace cumplir.
        reserva.ToTable("reservas", tabla =>
        {
            tabla.HasCheckConstraint("ck_reservas_estado",
                "estado IN ('retenida', 'confirmada', 'rechazada', 'vencida')");
            tabla.HasCheckConstraint("ck_reservas_motivo",
                "motivo_rechazo IN ('asiento_ocupado', 'pago_fallido', 'pago_no_disponible')");
            tabla.HasCheckConstraint("ck_reservas_estado_motivo",
                "(estado = 'rechazada' AND motivo_rechazo IS NOT NULL) OR (estado <> 'rechazada' AND motivo_rechazo IS NULL)");
            tabla.HasCheckConstraint("ck_reservas_vencimiento",
                "estado = 'rechazada' OR fecha_vencimiento IS NOT NULL");
            tabla.HasCheckConstraint("ck_reservas_usuario", "usuario_id ~ '[^[:space:]]'");
            tabla.HasCheckConstraint("ck_reservas_usuario_largo", "length(usuario_id) <= 200");
        });

        reserva.HasKey(r => r.Id).HasName("reservas_pkey");
        // Conserva la restricción UNIQUE original y su índice, sin crear otro índice duplicado.
        reserva.HasAlternateKey(r => r.SolicitudId).HasName("reservas_solicitud_id_key");
        reserva.Property(r => r.Id).HasColumnName("id").HasColumnType("uuid").ValueGeneratedNever();
        reserva.Property(r => r.SolicitudId).HasColumnName("solicitud_id").HasColumnType("uuid");
        reserva.Property(r => r.EventoId).HasColumnName("evento_id").HasColumnType("uuid");
        reserva.Property(r => r.AsientoId).HasColumnName("asiento_id").HasColumnType("uuid");
        reserva.Property(r => r.UsuarioId).HasColumnName("usuario_id").HasColumnType("text").IsRequired();

        reserva.Property(r => r.Estado).HasColumnName("estado").HasColumnType("text")
            .HasConversion(new ValueConverter<EstadoReserva, string>(
                valor => ATexto(valor, Estados), texto => DesdeTexto(texto, Estados)));
        reserva.Property(r => r.Motivo).HasColumnName("motivo_rechazo").HasColumnType("text")
            .HasConversion(new ValueConverter<MotivoRechazo, string>(
                valor => ATexto(valor, Motivos), texto => DesdeTexto(texto, Motivos)));

        // PostgreSQL es dueño de ambas fechas al insertar, incluso si C# trae un valor explícito.
        reserva.Property(r => r.FechaCreacion).HasColumnName("fecha_creacion")
            .HasColumnType("timestamp with time zone").HasDefaultValueSql("NOW()").ValueGeneratedOnAdd()
            .Metadata.SetBeforeSaveBehavior(PropertySaveBehavior.Ignore);
        reserva.Property(r => r.FechaActualizacion).HasColumnName("fecha_actualizacion")
            .HasColumnType("timestamp with time zone").HasDefaultValueSql("NOW()").ValueGeneratedOnAdd()
            .Metadata.SetBeforeSaveBehavior(PropertySaveBehavior.Ignore);
        reserva.Property(r => r.FechaVencimiento).HasColumnName("fecha_vencimiento")
            .HasColumnType("timestamp with time zone");

        reserva.HasIndex(r => new { r.EventoId, r.AsientoId }).IsUnique()
            .HasDatabaseName("ux_reserva_asiento_activo")
            .HasFilter("estado IN ('retenida', 'confirmada')");
        reserva.HasIndex(r => r.FechaVencimiento)
            .HasDatabaseName("ix_reservas_retenciones_vencimiento")
            .HasFilter("estado = 'retenida'");
    }

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
