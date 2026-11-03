// Matías De La Rosa
using Availability.UseCases.Solicitudes.SolicitarReserva.Models;

namespace Availability.Presenters.SolicitarReserva
{
    // ViewModel del resultado de una solicitud de reserva. Igual que en los ViewModels de
    // Eventos, los textos se deciden acá y no en el Interactor ni en la vista: el Interactor
    // informa qué pasó (Recibida, AsientoInexistente, PendienteDeEnvio) y este anillo decide
    // cómo se le cuenta al usuario.
    public class SolicitarReservaViewModel
    {
        public SolicitarReservaViewModel(OutputDataResponse response)
        {
            EventoId = response.EventoId;

            // Con el patrón Outbox, una solicitud pendiente de envío también quedó registrada:
            // tiene número y se puede seguir. Las dos van a la página de solicitud recibida.
            Registrada = response.Resultado is ResultadoSolicitud.Recibida or ResultadoSolicitud.PendienteDeEnvio;
            PendienteDeEnvio = response.Resultado == ResultadoSolicitud.PendienteDeEnvio;

            if (response.Resultado != ResultadoSolicitud.AsientoInexistente)
            {
                AsientoTexto = $"Fila {response.Fila}, asiento {response.Numero}";
            }

            switch (response.Resultado)
            {
                case ResultadoSolicitud.Recibida:
                case ResultadoSolicitud.PendienteDeEnvio:
                    SolicitudId = response.SolicitudId;
                    NumeroSolicitud = FormatearNumero(response.SolicitudId!.Value);
                    // Los mismos textos que la página de solicitud recibida, a la que redirige
                    // Availability.Web en estos dos casos (Post/Redirect/Get).
                    Titulo = SolicitudRecibidaViewModel.TituloSegun(PendienteDeEnvio);
                    Mensaje = SolicitudRecibidaViewModel.MensajeSegun(PendienteDeEnvio);
                    break;

                case ResultadoSolicitud.AsientoInexistente:
                    Titulo = "No encontramos ese asiento";
                    Mensaje = "El asiento que elegiste no pertenece a este evento. " +
                              "Volvé al mapa de la sala y elegí otro.";
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(response), response.Resultado, "Resultado de solicitud no contemplado.");
            }
        }

        // Si la solicitud quedó registrada, enviada o pendiente de envío. Availability.Web
        // redirige a la página de solicitud recibida cuando es true.
        public bool Registrada { get; internal set; }

        // Si quedó registrada pero el broker no respondió. La página lo aclara.
        public bool PendienteDeEnvio { get; internal set; }

        // Para volver al mapa de asientos del evento.
        public Guid EventoId { get; internal set; }

        // El identificador completo, para armar el enlace al seguimiento. Solo tiene valor
        // cuando la solicitud quedó registrada.
        public Guid? SolicitudId { get; internal set; }

        // El número que ve el usuario. Solo tiene valor cuando la solicitud quedó registrada.
        public string? NumeroSolicitud { get; internal set; }

        // Por ejemplo, "Fila A, asiento 2". Queda vacío si el asiento no existe.
        public string? AsientoTexto { get; internal set; }

        public string Titulo { get; internal set; } = string.Empty;

        public string Mensaje { get; internal set; } = string.Empty;

        // Un Guid completo (36 caracteres) es incómodo de leer o dictar. Al usuario se le
        // muestran los primeros 8 caracteres en mayúsculas, por ejemplo "C9E2F5A8", como el
        // código de una entrada. El Guid completo sigue viajando en SolicitudId para los
        // enlaces, así que el número corto es solo de presentación. Es público para que la
        // página de solicitud recibida muestre el mismo formato.
        public static string FormatearNumero(Guid solicitudId) =>
            solicitudId.ToString("N")[..8].ToUpperInvariant();
    }
}
