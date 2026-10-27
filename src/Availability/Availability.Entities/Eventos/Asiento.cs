namespace Availability.Entities.Eventos
{
    // Entidad de dominio: un asiento que existe en un evento. Anémica, solo datos.
    //
    // No tiene estado (libre, retenido, ocupado). Según la propuesta del profesor en la
    // Clase 21, Availability conoce qué asientos existen y Booking es el dueño de las
    // reservas, así que el estado de cada asiento se le consulta a Booking.
    //
    // No tiene EventoId. La relación con el evento se expresa con la lista Evento.Asientos;
    // la clave foránea es una decisión de la base y se agrega en la entidad de
    // persistencia de Availability.Gateways.
    public class Asiento
    {
        // El Id va en el dominio por la misma razón que el de Evento: identifica al asiento
        // ante otros servicios, porque viaja en la solicitud de reserva que se publica en el
        // broker y en las consultas de ocupación a Booking. Es Guid para que lo genere la
        // aplicación y no dependa del autoincremental de la base.
        public Guid Id { get; set; }

        // Fila y número son como el usuario nombra el asiento; se muestran en el sitio.
        // string porque en muchas salas las filas se nombran con letras (A, B, AA).
        public string Fila { get; set; } = string.Empty;

        public int Numero { get; set; }
    }
}
