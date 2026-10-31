// Matías De La Rosa
namespace Availability.UseCases.Solicitudes.SolicitarReserva.Models
{
    // Datos de salida del caso de uso que recibe el presenter. Usa una clase propia y no la
    // entidad SolicitudReserva, igual que los casos de uso de Eventos, para que un cambio en
    // el dominio no llegue al presenter.
    public class OutputDataResponse
    {
        public ResultadoSolicitud Resultado { get; set; }

        public Guid EventoId { get; set; }

        // Solo tiene valor cuando el resultado es Recibida: es el número con el que el
        // usuario va a seguir su solicitud (paso 7).
        public Guid? SolicitudId { get; set; }

        // Fila y número del asiento pedido, para que el presenter pueda mostrar "asiento A2".
        // Quedan vacíos cuando el asiento no existe.
        public string Fila { get; set; } = string.Empty;

        public int Numero { get; set; }
    }

    // Cómo terminó el pedido dentro de Availability. No dice si la reserva se concretó:
    // eso lo decide Booking después, y el usuario lo consulta con el seguimiento.
    public enum ResultadoSolicitud
    {
        // La solicitud se registró y se publicó en el broker. Queda en proceso.
        Recibida,

        // El evento no existe o el asiento no pertenece al evento.
        AsientoInexistente,

        // La solicitud no se pudo publicar porque el broker no respondió. No queda
        // registrada y el usuario puede volver a intentarlo.
        NoSePudoEnviar
    }
}
