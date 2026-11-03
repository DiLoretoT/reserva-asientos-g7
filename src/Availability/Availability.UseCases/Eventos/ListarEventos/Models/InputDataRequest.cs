namespace Availability.UseCases.Eventos.ListarEventos.Models
{
    // El listado de eventos no necesita datos de entrada: es público y muestra todos los
    // eventos. La clase existe igual para que el caso de uso tenga la misma forma que los
    // demás (IPort<InputDataRequest>), y si más adelante hace falta un filtro, por ejemplo
    // por fecha, se agrega acá sin cambiar los puertos.
    public class InputDataRequest
    {
    }
}
