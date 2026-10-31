// Matías De La Rosa
namespace Availability.Presenters.SolicitarReserva
{
    // ViewModel de la página "solicitud recibida", la que se ve después de reservar.
    //
    // Existe aparte de SolicitarReservaViewModel por el patrón Post/Redirect/Get: el
    // formulario de reserva hace un POST que ejecuta el caso de uso y, si la solicitud se
    // recibió, redirige con un GET a esta página. Así, si el usuario recarga la página, el
    // navegador repite el GET y no vuelve a enviar la solicitud. Como el GET es un pedido
    // nuevo, el ViewModel del POST ya no existe, y esta página se arma solo con el número de
    // solicitud de la dirección y los datos que Availability.Web guarda para el siguiente
    // pedido.
    //
    // Los textos son los mismos que los del caso exitoso de SolicitarReservaViewModel, que
    // los toma de acá para que no puedan quedar distintos.
    public class SolicitudRecibidaViewModel
    {
        public const string TituloRecibida = "Recibimos tu solicitud";

        // El texto aclara que todavía no hay reserva: Availability solo registró el pedido.
        // Booking decide después si el asiento sigue libre y cobra, y el resultado se ve en
        // el seguimiento (paso 7).
        public const string MensajeRecibida =
            "Tu reserva está en proceso. Estamos verificando que el asiento siga libre y " +
            "procesando el pago. En unos instantes vas a poder consultar el resultado con tu " +
            "número de solicitud.";

        // asientoTexto y eventoId pueden faltar, por ejemplo si el usuario vuelve a abrir la
        // página más tarde: en ese caso se muestra solo el número de solicitud.
        public SolicitudRecibidaViewModel(Guid solicitudId, string? asientoTexto, Guid? eventoId)
        {
            SolicitudId = solicitudId;
            NumeroSolicitud = SolicitarReservaViewModel.FormatearNumero(solicitudId);
            AsientoTexto = asientoTexto;
            EventoId = eventoId;
        }

        public Guid SolicitudId { get; }

        public string NumeroSolicitud { get; }

        public string? AsientoTexto { get; }

        public Guid? EventoId { get; }

        public string Titulo => TituloRecibida;

        public string Mensaje => MensajeRecibida;
    }
}
