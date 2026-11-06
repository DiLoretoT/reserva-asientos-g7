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
// Los mensajes duplicados (misma solicitud) devuelven la reserva existente, también si dos réplicas los procesan
// a la vez. La consulta previa por SolicitudId sola no alcanza: entre el "¿existe?" y el insert otra réplica se
// puede colar, igual que con el asiento, y la segunda terminaría con una excepción en vez de un resultado. Por
// eso la decisión final la toma otra vez la base: la restricción UNIQUE de solicitud_id deja pasar un solo
// insert, y la réplica que pierde lee la reserva de la ganadora y devuelve eso. Así el mismo mensaje da siempre
// el mismo resultado, sin reservas duplicadas y sin errores. Eso es idempotencia: el broker puede entregar el
// mensaje más de una vez (por ejemplo si una réplica cae antes de confirmarlo) y el solicitudId identifica que es
// la misma solicitud. Es la misma condición de carrera de la Clase 4, resuelta igual con la base como árbitro. Mantengo igual la consulta previa porque en el
// caso común (reentrega, no simultánea) evita un insert que sabemos que va a fallar.
//
// No libera las retenidas vencidas, eso es del proceso de vencimiento.
public class RetencionReservas(BookingDbContext db, IOptions<BookingOptions> opciones)
{
    // Nombre del índice de BookingDbContext, para saber que el error vino de ese índice y no de otro.
    private const string IndiceAsientoActivo = "ux_reserva_asiento_activo";

    // Restricción UNIQUE de solicitud_id: una solicitud no puede tener dos reservas.
    private const string RestriccionSolicitud = "reservas_solicitud_id_key";

    // Devuelve la reserva resultante: retenida si el asiento estaba libre, rechazada si estaba ocupado.
    public async Task<Reserva> RetenerORechazarAsync(SolicitudReserva solicitud)
    {
        // Si la solicitud ya tiene reserva la devuelvo y no creo otra, porque el broker puede entregar el mismo
        // mensaje dos veces. Es solo un atajo para el caso común: no es la garantía, esa es la restricción UNIQUE
        // (ver el catch más abajo).
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
        catch (DbUpdateException ex) when (EsViolacion(ex, RestriccionSolicitud))
        {
            // Otra réplica procesó el mismo mensaje entre mi consulta y mi insert: devuelvo su reserva. Este catch va
            // antes que el del asiento porque en ese caso también puede chocar el asiento (el de la otra réplica es
            // el mismo), y lo correcto es devolver la reserva de la solicitud, no rechazarla por "asiento ocupado".
            db.Entry(retenida).State = EntityState.Detached;
            return await ObtenerExistenteAsync(solicitud);
        }
        catch (DbUpdateException ex) when (EsViolacion(ex, IndiceAsientoActivo))
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
        try
        {
            await db.SaveChangesAsync();
            return rechazada;
        }
        catch (DbUpdateException ex) when (EsViolacion(ex, RestriccionSolicitud))
        {
            // Mismo caso: otra réplica ya registró esta solicitud. Sin este catch, un duplicado simultáneo de una
            // solicitud rechazada fallaría con excepción al guardar la segunda rechazada.
            db.Entry(rechazada).State = EntityState.Detached;
            return await ObtenerExistenteAsync(solicitud);
        }
    }

    // Lee la reserva ya guardada de la solicitud. Va sin tracking para no arrastrar entidades descartadas.
    private async Task<Reserva> ObtenerExistenteAsync(SolicitudReserva solicitud)
        => await db.Reservas.AsNoTracking().FirstAsync(r => r.SolicitudId == solicitud.SolicitudId);

    // Se compara el nombre de la restricción porque las dos (asiento y solicitud) dan el mismo SqlState de
    // unicidad y significan cosas distintas: una es "asiento ocupado" y la otra es "mensaje duplicado".
    // Solo una violación de unicidad de esa restricción es esperable. Cualquier otro error,
    // como una caída de la base, tiene que salir como error y no como un rechazo falso.
    private static bool EsViolacion(DbUpdateException ex, string restriccion)
        => ex.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation
        } pg && pg.ConstraintName == restriccion;
}
