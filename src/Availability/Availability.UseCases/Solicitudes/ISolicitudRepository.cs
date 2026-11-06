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

        // Hasta maximo solicitudes que siguen pendientes de envío y se registraron antes de
        // registradasAntesDe, de la más vieja a la más nueva. Lo usa ReintentarEnvio. El
        // límite de fecha deja afuera las recién creadas, que SolicitarReserva todavía puede
        // estar publicando.
        Task<List<SolicitudReserva>> ListarPendientesDeEnvioAsync(DateTime registradasAntesDe, int maximo);

        // Marca la solicitud como enviada, después de que el broker confirmó la publicación.
        Task MarcarEnviadaAsync(Guid solicitudId);
    }
}
