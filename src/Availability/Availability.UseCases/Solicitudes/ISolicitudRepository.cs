using Availability.Entities.Solicitudes;

namespace Availability.UseCases.Solicitudes
{
    // Puerto de salida para las solicitudes de reserva. Igual que IEventoRepository, lo
    // define el anillo de casos de uso según lo que necesita, y lo implementa
    // Availability.Gateways con la base de datos.
    public interface ISolicitudRepository
    {
        // Registra una solicitud nueva (paso 3). El caso de uso la guarda antes de
        // publicarla en el broker: si se publicara primero y la base fallara, Booking
        // procesaría una solicitud que Availability no conoce y no podría mostrar.
        Task AgregarAsync(SolicitudReserva solicitud);

        // Devuelve la solicitud registrada, o null si no existe. Lo usa el seguimiento
        // (paso 7): si existe acá y Booking todavía no la tiene, se muestra "en proceso".
        Task<SolicitudReserva?> ObtenerAsync(Guid solicitudId);

        // Borra una solicitud registrada. Lo usa SolicitarReserva como acción compensatoria
        // cuando la solicitud se guardó pero no se pudo publicar en el broker: así no queda
        // una solicitud que Booking nunca va a recibir y que el seguimiento mostraría "en
        // proceso" para siempre.
        Task EliminarAsync(Guid solicitudId);
    }
}
