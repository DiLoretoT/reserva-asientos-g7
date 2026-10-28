// Tomás Di Loreto
using Availability.Entities.Eventos;

namespace Availability.UseCases.Interfaces
{
    // Puerto hacia donde se guardan los eventos. Definido por este anillo e implementado por 
    // una clase en un anillo más afuera. En principio se implementa el caso de uso que se
    // necesita, que es uno que trae el evento y sus asientos. Si a futuro se necesito otra 
    // cosa, se implementan nuevos casos de uso. 

    public interface IEventoRepository
    {
        // Devuelve el evento con todos sus asientos o null si no existe un evento con ese id.
        // Devuelve la entidad de dominio Evento y no una clase de persistencia, ya que la 
        // conversión entre ambas la hace el gateway

        Task<Evento?> ObtenerPorIdAsync(Guid eventoId);
    }
}