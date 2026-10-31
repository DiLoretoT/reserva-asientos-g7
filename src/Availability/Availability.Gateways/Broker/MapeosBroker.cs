// Matías De La Rosa
using Availability.Entities.Solicitudes;

namespace Availability.Gateways.Broker
{
    // Traduce la entidad de dominio al mensaje del broker. Así el caso de uso trabaja con
    // SolicitudReserva y no depende del formato del mensaje. Sigue la convención de
    // Persistencia/Mapeos.cs y Booking/MapeosBooking.cs: métodos de extensión escritos a mano.
    internal static class MapeosBroker
    {
        public static SolicitudReservaMensaje AMensaje(this SolicitudReserva solicitud) =>
            new()
            {
                SolicitudId = solicitud.Id,
                EventoId = solicitud.EventoId,
                AsientoId = solicitud.AsientoId,
                UsuarioId = solicitud.UsuarioId,
                FechaSolicitud = solicitud.FechaSolicitud
            };
    }
}
