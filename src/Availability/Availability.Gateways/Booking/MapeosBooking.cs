// Tomás Di Loreto
using Availability.UseCases.Interfaces;

namespace Availability.Gateways.Booking
{
    // Traduce la respuesta de Booking (la forma del contrato) a ResultadoOcupacion 
    // (la forma que pide el caso de uso). Así el interactor no depende del formato del 
    // JSON. Si el contrato cambia, se toca este archivo y AsientosOcupadosResponse, no 
    // el anillo 2. Sigue la convención de Persistencia/Mapeos.cs: métodos de extensión 
    // escritos a mano.
    internal static class MapeosBooking
    {
        // RespuestaOk se marca acá porque solo se llega a este punto si Booking respondió
        // con datos válidos. Los casos de error los resuelve quien llama, sin pasar por acá.
        public static ResultadoOcupacion AResultado(this AsientosOcupadosResponse respuesta) =>
            new()
            {
                RespuestaOk = true,
                AsientosOcupados = respuesta.AsientosOcupados.Select(a => a.AOcupado()).ToList()
            };
        private static AsientoOcupado AOcupado(this AsientoOcupadoResponse asiento) =>
            new()
            {
                AsientoId = asiento.AsientoId,
                Estado = AEstado(asiento.Estado)
            };
        // El contrato define solo "retenida" y "confirmada". Otro valor significa que Booking
        // cambió el contrato y se lanza FormatException y quien llama lo trata como respuesta
        // inválida (asientos como "Desconocido"), en vez de suponer un estado.
        private static EstadoOcupacion AEstado(string estado) =>
            estado switch
            {
                "retenida" => EstadoOcupacion.Retenida,
                "confirmada" => EstadoOcupacion.Confirmada,
                _ => throw new FormatException($"Estado de ocupación fuera del contrato: '{estado}'.")
            };
    }
    
}