using System.ComponentModel.DataAnnotations;
using Availability.Entities.Solicitudes;

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

        // Patrón Outbox: Pendiente hasta que el broker confirma la publicación. Se guarda como
        // texto ("Pendiente", "Enviada") y no como número, así la tabla se entiende al leerla
        // y agregar un valor al enum no cambia el significado de las filas guardadas.
        public EstadoEnvio EstadoEnvio { get; set; }
    }
}
