// Tomás Di Loreto
namespace Availability.Entities.Eventos
{
    // Entidad de dominio: un evento con asientos para reservar. El dominio es
    // abstracto a propósito, porque el sistema no es para un cine ni un recital en particular.
    //
    // Es anémica, igual que Agenda.Entities.Todo.Task del ejemplo de la cátedra:
    // solo propiedades, sin métodos ni validaciones. Las reglas (por ejemplo, que
    // el asiento pedido exista) van en los casos de uso de Availability.UseCases.
    //
    // No tiene atributos de base de datos ([Key], [Required]) ni Timestamp. Eso vive
    // en la entidad de persistencia de Availability.Gateways, igual que en el ejemplo,
    // para que este anillo no dependa de cómo ni dónde se guardan los datos.
    public class Evento
    {
        // El Id sí va en el dominio, a diferencia del Id int del ejemplo. Acá no es solo
        // la clave de una tabla: identifica al evento ante otros servicios, porque viaja
        // en la solicitud de reserva que se publica en el broker y en las consultas a Booking.
        // Es Guid para que lo genere la aplicación y no dependa del autoincremental de la base.
        public Guid Id { get; set; }

        public string Nombre { get; set; } = string.Empty;

        public DateTime Fecha { get; set; }

        // Los asientos que existen en el evento, es decir, su capacidad física.
        // La relación se expresa con la lista y no con claves foráneas, que son
        // un detalle de la persistencia.
        public List<Asiento> Asientos { get; set; } = new();
    }
}
