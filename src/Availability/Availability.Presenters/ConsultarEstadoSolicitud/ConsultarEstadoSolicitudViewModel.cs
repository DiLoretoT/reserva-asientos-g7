// Tomás Di Loreto
using Availability.Presenters.Comun;
using Availability.UseCases.Interfaces;
using Availability.UseCases.Solicitudes.ConsultarEstadoSolicitud.Models;

namespace Availability.Presenters.ConsultarEstadoSolicitud
{
    // ViewModel de la página de seguimiento de una solicitud. Los textos de cada estado y de
    // cada motivo, las clases CSS y el formato de las fechas se deciden acá, en el anillo 3, y
    // la vista solo los muestra, igual que en la página de asientos.
    public class ConsultarEstadoSolicitudViewModel
    {
        public ConsultarEstadoSolicitudViewModel(OutputDataResponse response)
        {
            SolicitudEncontrada = response.SolicitudEncontrada;

            // El mismo mensaje para una solicitud inexistente y para una de otro usuario, así
            // la página no confirma que exista una solicitud ajena.
            if (!response.SolicitudEncontrada)
            {
                MensajeNoEncontrada =
                    "No encontramos esta solicitud entre las tuyas. Revisá que el enlace sea correcto.";
                return;
            }

            NumeroSolicitud = response.SolicitudId.ToString();
            NombreEvento = response.NombreEvento;
            FechaEventoTexto = FormatoFechas.Completa(response.FechaEvento);
            AsientoTexto = $"Fila {response.Fila}, asiento {response.NumeroAsiento}";
            FechaSolicitudTexto = FormatoFechas.Completa(response.FechaSolicitud);

            (EstadoTexto, EstadoDescripcion, ClaseCss) = Presentacion(response.Estado);

            if (response.Estado == EstadoSolicitud.Rechazada && response.Motivo is not null)
            {
                MotivoTexto = TextoMotivo(response.Motivo.Value);
            }

            if (response.Estado == EstadoSolicitud.Desconocido)
            {
                AvisoTitulo = "No pudimos consultar el estado";
                Aviso =
                    "En este momento no podemos saber cómo sigue tu solicitud. Tu pedido sigue " +
                    "registrado. Volvé a intentar en unos minutos.";
            }
        }

        public bool SolicitudEncontrada { get; internal set; }

        public string? MensajeNoEncontrada { get; internal set; }

        public string NumeroSolicitud { get; internal set; } = string.Empty;

        public string NombreEvento { get; internal set; } = string.Empty;

        public string FechaEventoTexto { get; internal set; } = string.Empty;

        public string AsientoTexto { get; internal set; } = string.Empty;

        public string FechaSolicitudTexto { get; internal set; } = string.Empty;

        public string EstadoTexto { get; internal set; } = string.Empty;

        public string EstadoDescripcion { get; internal set; } = string.Empty;

        public string ClaseCss { get; internal set; } = string.Empty;

        public string? MotivoTexto { get; internal set; }

        public string? AvisoTitulo { get; internal set; }

        public string? Aviso { get; internal set; }

        // Texto corto, explicación y clase de cada estado. Las clases son las del CSS propio del
        // sitio. El estado se lee también en el texto, no solo en el color.
        private static (string Texto, string Descripcion, string ClaseCss) Presentacion(EstadoSolicitud estado) =>
            estado switch
            {
                EstadoSolicitud.EnProceso => ("En proceso",
                    "Recibimos tu solicitud y todavía se está procesando. Volvé a consultar en unos minutos.",
                    "insignia--en-proceso"),
                EstadoSolicitud.Retenida => ("Pendiente de pago",
                    "Tu asiento está retenido mientras se procesa el pago.",
                    "insignia--pendiente"),
                EstadoSolicitud.Confirmada => ("Confirmada",
                    "Tu reserva está confirmada.",
                    "insignia--confirmada"),
                EstadoSolicitud.Rechazada => ("Rechazada",
                    "Tu reserva no se pudo concretar.",
                    "insignia--rechazada"),
                EstadoSolicitud.Vencida => ("Vencida",
                    "El pago no se completó a tiempo y el asiento quedó libre.",
                    "insignia--vencida"),
                EstadoSolicitud.Desconocido => ("Sin información",
                    "No sabemos el estado de la solicitud en este momento.",
                    "insignia--sin-info"),
                _ => throw new ArgumentOutOfRangeException(nameof(estado), estado, "Estado de solicitud no contemplado.")
            };

        private static string TextoMotivo(MotivoRechazo motivo) =>
            motivo switch
            {
                MotivoRechazo.AsientoOcupado => "El asiento ya estaba ocupado cuando se procesó tu solicitud.",
                MotivoRechazo.PagoFallido => "El pago no se aprobó.",
                MotivoRechazo.PagoNoDisponible => "El servicio de pagos no respondió y no se pudo cobrar.",
                _ => throw new ArgumentOutOfRangeException(nameof(motivo), motivo, "Motivo de rechazo no contemplado.")
            };
    }
}