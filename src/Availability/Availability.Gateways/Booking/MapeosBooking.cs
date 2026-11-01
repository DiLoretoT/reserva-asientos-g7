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
        
        // Endpoint 2. Igual que en el endpoint 1, solo se llega acá con una respuesta 200, así
        // que RespuestaOk y Procesada son true. El 404 lo resuelve ClienteBooking.
        public static ResultadoEstadoSolicitud AResultado(this EstadoSolicitudResponse respuesta) =>
            new()
            {
                RespuestaOk = true,
                Procesada = true,
                Estado = AEstadoReserva(respuesta.Estado),
                Motivo = respuesta.Motivo is null ? null : AMotivo(respuesta.Motivo)
            };

        // Mismo criterio que AEstado: un valor que no está en el contrato lanza FormatException
        // y la página avisa que no se pudo consultar, en vez de mostrar un estado supuesto.
        private static EstadoReserva AEstadoReserva(string estado) =>
            estado switch
            {
                "retenida" => EstadoReserva.Retenida,
                "confirmada" => EstadoReserva.Confirmada,
                "rechazada" => EstadoReserva.Rechazada,
                "vencida" => EstadoReserva.Vencida,
                _ => throw new FormatException($"Estado de reserva fuera del contrato: '{estado}'.")
            };

        private static MotivoRechazo AMotivo(string motivo) =>
            motivo switch
            {
                "asiento_ocupado" => MotivoRechazo.AsientoOcupado,
                "pago_fallido" => MotivoRechazo.PagoFallido,
                "pago_no_disponible" => MotivoRechazo.PagoNoDisponible,
                _ => throw new FormatException($"Motivo de rechazo fuera del contrato: '{motivo}'.")
            };
    }
    
}