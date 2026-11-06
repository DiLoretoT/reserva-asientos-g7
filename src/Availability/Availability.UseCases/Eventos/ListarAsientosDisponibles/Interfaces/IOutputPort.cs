// Tomás Di Loreto
using Availability.UseCases.Eventos.ListarAsientosDisponibles.Models;
using Availability.UseCases.Interfaces;

namespace Availability.UseCases.Eventos.ListarAsientosDisponibles.Interfaces
{
    // Puerto de salida del caso de uso. Lo define este anillo y lo implementa el presenter
    // en Availability.Presenters, así el interactor entrega el resultado y el presenter se ocupa de mostrarlo. 
    
    public interface IOutputPort : IPort<OutputDataResponse>
    {

    }
}