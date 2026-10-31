// Matías De La Rosa
using Availability.UseCases.Solicitudes.SolicitarReserva.Models;

namespace Availability.Presenters.SolicitarReserva
{
    // ViewModel del resultado de una solicitud de reserva. Igual que en los ViewModels de
    // Eventos, los textos se deciden acá y no en el Interactor ni en la vista: el Interactor
    // informa qué pasó (Recibida, AsientoInexistente, NoSePudoEnviar) y este anillo decide
    // cómo se le cuenta al usuario.
    public class SolicitarReservaViewModel
    {
        public SolicitarReservaViewModel(OutputDataResponse response)
        {
            EventoId = response.EventoId;
            Recibida = response.Resultado == ResultadoSolicitud.Recibida;

            if (response.Resultado != ResultadoSolicitud.AsientoInexistente)
            {
                AsientoTexto = $"Fila {response.Fila}, asiento {response.Numero}";
            }

            switch (response.Resultado)
            {
                case ResultadoSolicitud.Recibida:
                    SolicitudId = response.SolicitudId;
                    NumeroSolicitud = FormatearNumero(response.SolicitudId!.Value);
                    // Los mismos textos que la página de solicitud recibida, a la que redirige
                    // Availability.Web en este caso (Post/Redirect/Get).
                    Titulo = SolicitudRecibidaViewModel.TituloRecibida;
                    Mensaje = SolicitudRecibidaViewModel.MensajeRecibida;
                    break;

                case ResultadoSolicitud.AsientoInexistente:
                    Titulo = "No encontramos ese asiento";
                    Mensaje = "El asiento que elegiste no pertenece a este evento. " +
                              "Volvé al mapa de la sala y elegí otro.";
                    break;

                case ResultadoSolicitud.NoSePudoEnviar:
                    Titulo = "No pudimos enviar tu solicitud";
                    // Se aclara que el pedido no quedó registrado, porque el caso de uso lo
                    // borró (acción compensatoria), así el usuario sabe que puede reintentar.
                    Mensaje = "En este momento no podemos recibir reservas y tu pedido no " +
                              "quedó registrado. Volvé a intentarlo en unos minutos.";
                    PuedeReintentar = true;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(response), response.Resultado, "Resultado de solicitud no contemplado.");
            }
        }

        public bool Recibida { get; internal set; }

        // Para volver al mapa de asientos del evento.
        public Guid EventoId { get; internal set; }

        // El identificador completo, para armar el enlace al seguimiento. Solo tiene valor
        // cuando la solicitud se recibió.
        public Guid? SolicitudId { get; internal set; }

        // El número que ve el usuario. Solo tiene valor cuando la solicitud se recibió.
        public string? NumeroSolicitud { get; internal set; }

        // Por ejemplo, "Fila A, asiento 2". Queda vacío si el asiento no existe.
        public string? AsientoTexto { get; internal set; }

        public string Titulo { get; internal set; } = string.Empty;

        public string Mensaje { get; internal set; } = string.Empty;

        // Si conviene ofrecer volver a intentar: solo cuando falló el broker, porque el
        // mismo pedido puede funcionar unos minutos después.
        public bool PuedeReintentar { get; internal set; }

        // Un Guid completo (36 caracteres) es incómodo de leer o dictar. Al usuario se le
        // muestran los primeros 8 caracteres en mayúsculas, por ejemplo "C9E2F5A8", como el
        // código de una entrada. El Guid completo sigue viajando en SolicitudId para los
        // enlaces, así que el número corto es solo de presentación. Es público para que la
        // página de solicitud recibida muestre el mismo formato.
        public static string FormatearNumero(Guid solicitudId) =>
            solicitudId.ToString("N")[..8].ToUpperInvariant();
    }
}
