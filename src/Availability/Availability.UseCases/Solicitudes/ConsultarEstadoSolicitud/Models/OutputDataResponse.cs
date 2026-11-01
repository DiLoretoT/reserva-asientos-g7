// Tomás Di Loreto
using Availability.UseCases.Interfaces;

namespace Availability.UseCases.Solicitudes.ConsultarEstadoSolicitud.Models
{
    // Datos de salida del caso de uso que recibe el presenter. Usa clases propias de este caso
    // de uso y no expone SolicitudReserva, Evento ni Asiento.
    public class OutputDataResponse
    {
        // false si la solicitud no existe o es de otro usuario. Los dos casos se informan
        // igual, para que nadie puede averiguar si existe una solicitud ajena probando ids.
        public bool SolicitudEncontrada { get; set; }

        public Guid SolicitudId { get; set; }

        public DateTime FechaSolicitud { get; set; }

        public string NombreEvento { get; set; } = string.Empty;

        public DateTime FechaEvento { get; set; }

        public string Fila { get; set; } = string.Empty;

        public int NumeroAsiento { get; set; }

        public EstadoSolicitud Estado { get; set; }

        public MotivoRechazo? Motivo { get; set; }
    }

    // Estados de la solicitud para la pantalla. Suma dos más a los cuatro del contrato:
    // EnProceso, cuando Booking respondió 404, y Desconocido, cuando Booking no respondió.
    public enum EstadoSolicitud
    {
        EnProceso,
        Retenida,
        Confirmada,
        Rechazada,
        Vencida,
        Desconocido
    }
}