// Tomás Di Loreto
namespace Availability.UseCases.Eventos.ListarAsientosDisponibles.Models
{
    // Datos de entrada del caso de uso. Se requiere únicamente el evento.
    // El usuario no inició sesión, así que no se envían datos del usuario. 

    public class InputDataRequest
    {
        public Guid EventoId {get; set; }
    }
}