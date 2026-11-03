// Tomás Di Loreto
namespace Availability.UseCases.Eventos.ListarAsientosDisponibles.Models
{
    // Datos de salida del caso de uso que recibe el presenter. 
    // No expone evento  ni asiento sino que usa clases propias de este caso de uso. 
    
    public class OutputDataResponse
    {
        // Indica si el evento pedido existe en Availability o si es inexistente, 
        // para que el presenter lo muestre como no encontrado. 

        public bool EventoEncontrado { get; set; }

        public Guid EventoId { get; set; }

        public string NombreEvento { get; set; } = string.Empty;

        public DateTime Fecha { get; set; }

        // Indica si se pudo consultar a Booking qué asientos están ocupados para poder 
        // mostrar un aviso del problema en caso de no haberse podido, priorizando la 
        // disponibilidad, como plantea el teorema de CAP. 
        public bool OcupacionDisponible { get; set; }

        // Todos los asientos del evento con su estado (disponible, pendiente, reservado o desconocido)
        public List<AsientoDelEvento> Asientos { get; set; } = new();        
    }

    public class AsientoDelEvento
    {
        // El id viaja para que en el caso de uso de reservar, la página indique qué 
        // asiento eligió el visitante.
        public Guid Id { get; set; } 
        public string Fila { get; set; } = string.Empty;
        public int Numero { get; set; }
        public EstadoAsiento Estado { get; set; }
    }

    // Estados de un asiento para la pantalla. El interactor los traduce desde el
    // contrato con Booking: un asiento que Booking no informa como ocupado es
    // Disponible, "retenida" es Pendiente y "confirmada" es Reservado.

    public enum EstadoAsiento
    {
        Disponible,
        Pendiente,
        Reservado,
        // En caso de no recibir la información de Booking se agrega el estado "Desconocido"
        Desconocido
    }
}