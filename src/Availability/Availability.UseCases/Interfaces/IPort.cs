// Tomás Di Loreto
namespace Availability.UseCases.Interfaces
{
    // Contrato común de los puertos de entrada y de salida.
    // T es el modelo que cruza el puerto InputDataRequest en el de entrada
    // y OutputDataResponse en el de salida. 

    public interface IPort<T>
    {
        Task HandleAsync(T input);
    }
}
