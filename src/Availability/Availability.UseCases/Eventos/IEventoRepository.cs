using Availability.Entities.Eventos;

namespace Availability.UseCases.Eventos
{
    // Puerto de salida (patrón Ports & Adapters): describe qué datos de eventos necesitan
    // los casos de uso, sin decir de dónde salen. La implementación (el adaptador) vive en
    // Availability.Gateways y usa la base de datos.
    //
    // La interfaz está en UseCases y no en Gateways por la regla de dependencia de Clean
    // Architecture: Gateways referencia a UseCases y nunca al revés. Así los casos de uso
    // no dependen de Entity Framework ni del motor de base de datos, y en las pruebas o
    // mientras falta la base se puede usar otra implementación.
    //
    // Patrón Repository: los casos de uso trabajan con entidades de dominio (Evento,
    // Asiento) como si fueran una colección en memoria, y el repositorio se encarga de
    // traducirlas desde y hacia las tablas.
    public interface IEventoRepository
    {
        // Lista los eventos para la pantalla de inicio. Devuelve los eventos sin sus
        // asientos (lista Asientos vacía), porque para listarlos no hacen falta y cargar
        // todos los asientos de todos los eventos sería una consulta innecesariamente pesada.
        Task<List<Evento>> ListarAsync();

        // Devuelve el evento con todos sus asientos, o null si no existe. Lo usan el
        // listado de asientos disponibles (paso 1) y la verificación de que el asiento
        // pedido exista antes de registrar la solicitud (paso 3).
        Task<Evento?> ObtenerConAsientosAsync(Guid eventoId);
    }
}
