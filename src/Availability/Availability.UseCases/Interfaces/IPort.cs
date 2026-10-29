// Tomás Di Loreto
namespace Availability.UseCases.Interfaces
{
    // Contrato común de los puertos de entrada y de salida.
    // T es el modelo que cruza el puerto InputDataRequest en el de entrada
    // y OutputDataResponse en el de salida.
    // A diferencia del ejemplo de clase, solo tiene la versión asincrónica, porque los
    // casos de uso consultan a otros servicios por la red.

    public interface IPort<T>
    {
        Task HandleAsync(T input);
    }
}
