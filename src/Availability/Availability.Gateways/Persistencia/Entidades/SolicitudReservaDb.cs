using System.ComponentModel.DataAnnotations;

namespace Availability.Gateways.Persistencia.Entidades
{
    // Entidad de persistencia: la forma de la solicitud de reserva en la tabla Solicitudes.
    //
    // EventoId y AsientoId son claves foráneas reales en la base: impiden guardar una
    // solicitud de un evento o asiento que no existe, como segunda defensa además de la
    // verificación que hace el caso de uso.
    public class SolicitudReservaDb
    {
        [Key]
        public Guid Id { get; set; }

        public Guid EventoId { get; set; }

        public EventoDb Evento { get; set; } = null!;

        public Guid AsientoId { get; set; }

        public AsientoDb Asiento { get; set; } = null!;

        [Required, MaxLength(200)]
        public string UsuarioId { get; set; } = string.Empty;

        public DateTime FechaSolicitud { get; set; }
    }
}
