using Booking.Data;
using Booking.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Booking.Business;

// Capa de negocio de Booking (N-Layer). Hace el paso 4 del flujo: si el asiento está libre crea una reserva
// retenida, y si está ocupado la rechaza. Las reglas están acá y no en la entidad Reserva porque el dominio es
// anémico. Recibe el DbContext por el constructor y lo usa directo, igual que ConsultaReservas.
//
// El problema es que varias réplicas de Booking pueden pedir el mismo asiento a la vez (condición de carrera).
// Si primero preguntara "¿está libre?" y después insertara, otra réplica se podría colar en el medio y las dos
// lo retendrían. Por eso inserto directo y dejo que decida la base de datos: el índice único
// ux_reserva_asiento_activo no permite dos reservas retenidas o confirmadas del mismo asiento. Las réplicas solo
// comparten la base, así que la garantía tiene que estar ahí. Lo preferí a un lock o a una versión por fila
// porque es lo más simple y no necesita una fila previa del asiento (los asientos son de Availability).
//
// No libera las retenidas vencidas, eso es del proceso de vencimiento.
public class RetencionReservas(BookingDbContext db, IOptions<BookingOptions> opciones)
{
    // Nombre del índice de BookingDbContext, para saber que el error vino de ese índice y no de otro.
    private const string IndiceAsientoActivo = "ux_reserva_asiento_activo";

    // Devuelve la reserva resultante: retenida si el asiento estaba libre, rechazada si estaba ocupado.
    public async Task<Reserva> RetenerORechazarAsync(SolicitudReserva solicitud)
    {
        // Si la solicitud ya tiene reserva la devuelvo y no creo otra, porque el broker puede entregar el mismo
        // mensaje dos veces. El manejo completo de duplicados es del requerimiento 17.
        var existente = await db.Reservas.FirstOrDefaultAsync(r => r.SolicitudId == solicitud.SolicitudId);
        if (existente is not null)
            return existente;

        // Reserva retenida: el vencimiento sale de la configuración para cambiarlo sin tocar el código.
        var retenida = new Reserva
        {
            Id = Guid.NewGuid(),
            SolicitudId = solicitud.SolicitudId,
            EventoId = solicitud.EventoId,
            AsientoId = solicitud.AsientoId,
            UsuarioId = solicitud.UsuarioId,
            Estado = EstadoReserva.Retenida,
            FechaVencimiento = DateTime.UtcNow.AddMinutes(opciones.Value.RetentionMinutes)
        };
        db.Reservas.Add(retenida);

        try
        {
            // Si el asiento estaba libre, se guarda y listo.
            await db.SaveChangesAsync();
            return retenida;
        }
        catch (DbUpdateException ex) when (EsAsientoOcupado(ex))
        {
            // El índice la rechazó: el asiento ya tiene una reserva vigente. La saco del contexto para que no
            // se vuelva a intentar guardar.
            db.Entry(retenida).State = EntityState.Detached;
        }

        // Guardo la solicitud como rechazada para que se pueda consultar el motivo. Esta sí entra, porque el
        // índice solo mira las retenidas y confirmadas, y no necesita vencimiento.
        var rechazada = new Reserva
        {
            Id = Guid.NewGuid(),
            SolicitudId = solicitud.SolicitudId,
            EventoId = solicitud.EventoId,
            AsientoId = solicitud.AsientoId,
            UsuarioId = solicitud.UsuarioId,
            Estado = EstadoReserva.Rechazada,
            Motivo = MotivoRechazo.AsientoOcupado
        };
        db.Reservas.Add(rechazada);
        await db.SaveChangesAsync();
        return rechazada;
    }

    // Solo ese error significa "asiento ocupado" (violación de unicidad). Cualquier otro,
    // como una caída de la base, tiene que salir como error y no como un rechazo falso.
    private static bool EsAsientoOcupado(DbUpdateException ex)
        => ex.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: IndiceAsientoActivo
        };
}
