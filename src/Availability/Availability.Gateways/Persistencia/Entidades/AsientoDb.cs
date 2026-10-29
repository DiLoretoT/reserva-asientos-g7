using System.ComponentModel.DataAnnotations;

namespace Availability.Gateways.Persistencia.Entidades
{
    // Entidad de persistencia: la forma del asiento en la tabla Asientos.
    //
    // Acá aparece la clave foránea EventoId, que la entidad de dominio Asiento no tiene a
    // propósito: en el dominio la relación se expresa con la lista Evento.Asientos y la
    // clave foránea es un detalle de cómo se guarda en la base.
    public class AsientoDb
    {
        [Key]
        public Guid Id { get; set; }

        [Required, MaxLength(5)]
        public string Fila { get; set; } = string.Empty;

        public int Numero { get; set; }

        // Clave foránea hacia Eventos.
        public Guid EventoId { get; set; }

        public EventoDb Evento { get; set; } = null!;
    }
}
