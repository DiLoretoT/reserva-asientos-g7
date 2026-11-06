using System.ComponentModel.DataAnnotations;

namespace Availability.Gateways.Persistencia.Entidades
{
    // Entidad de persistencia: la forma del evento en la tabla Eventos.
    //
    // Está separada de Availability.Entities.Eventos.Evento, igual que en el ejemplo de la
    // cátedra (Agenda.Entities contra Agenda.Gateways). La entidad de dominio no sabe nada
    // de la base; esta sí tiene los atributos de base de datos ([Key], [Required],
    // [MaxLength]). Si mañana cambia la tabla, cambia esta clase y el dominio no se entera.
    public class EventoDb
    {
        [Key]
        public Guid Id { get; set; }

        [Required, MaxLength(200)]
        public string Nombre { get; set; } = string.Empty;

        public DateTime Fecha { get; set; }

        // Propiedad de navegación de Entity Framework: le dice que un evento tiene muchos
        // asientos (relación uno a muchos con AsientoDb.EventoId).
        public List<AsientoDb> Asientos { get; set; } = new();
    }
}
