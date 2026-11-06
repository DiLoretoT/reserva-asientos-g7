// Matías De La Rosa
using Availability.Entities.Solicitudes;

namespace Availability.UseCases.Interfaces
{
    // Puerto de salida para publicar una solicitud de reserva en el broker (paso 3 del
    // enunciado). Lo define este anillo y lo implementa una clase de Availability.Gateways:
    // hoy una simulación, y cuando tengamos el connection string, el cliente de Azure
    // Service Bus. El caso de uso no sabe qué broker hay detrás ni cómo es el mensaje.
    //
    // Recibe la entidad de dominio y no el mensaje, porque la forma del mensaje la define el
    // contrato con Booking (issue #44) y es un detalle de Gateways: si el contrato cambia,
    // cambia el adaptador y este puerto no.
    public interface IPublicadorSolicitudes
    {
        // Devuelve true si el broker aceptó el mensaje y false si no respondió.
        //
        // Igual que IConsultaOcupacion, la implementación no propaga la excepción cuando el
        // broker no está disponible: en un sistema distribuido es una situación esperada, y
        // con un bool la firma obliga al Interactor a contemplarla.
        Task<bool> PublicarAsync(SolicitudReserva solicitud);
    }
}
