// Tomás Di Loreto
using Availability.Entities.Eventos;

namespace Availability.UseCases.Interfaces
{
    // Puerto hacia donde se guardan los eventos. Definido por este anillo e implementado por 
    // una clase en un anillo más afuera. En principio se implementa el método que se
    // necesita, que es uno que trae el evento y sus asientos. Si a futuro se necesita otra 
    // cosa, se implementan nuevos métodos. 
    // Es específico de eventos y no genérico como IRepository<T>, porque los 
    // asientos siempre se consultan junto con su evento.

    public interface IEventoRepository
    {
        // Devuelve el evento con todos sus asientos o null si no existe un evento con ese id.
        // Devuelve la entidad de dominio Evento y no una clase de persistencia, ya que la 
        // conversión entre ambas la hace el gateway

        Task<Evento?> ObtenerPorIdAsync(Guid eventoId);
    }
}