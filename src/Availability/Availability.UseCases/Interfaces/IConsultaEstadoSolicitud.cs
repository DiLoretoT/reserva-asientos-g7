// Tomás Di Loreto
namespace Availability.UseCases.Interfaces
{
    // Puerto para consultarle a Booking el estado de una solicitud (endpoint 2 del contrato).
    // Va junto a IConsultaOcupacion porque los dos describen lo que Availability necesita de
    // Booking. Lo implementa el cliente HTTP de Availability.Gateways.
    public interface IConsultaEstadoSolicitud
    {
        Task<ResultadoEstadoSolicitud> ObtenerEstadoAsync(Guid solicitudId);
    }

    public class ResultadoEstadoSolicitud
    {
        public bool RespuestaOk { get; set; }

        // false cuando Booking respondió 404, es decir, todavía no procesó la solicitud. Se
        // separa de RespuestaOk porque el 404 es una respuesta válida y no una falla
        // (consistencia eventual, ver el contrato REST).
        public bool Procesada { get; set; }

        // Solo tiene sentido si RespuestaOk y Procesada son true.
        public EstadoReserva Estado { get; set; }

        // Solo tiene valor si Estado es Rechazada.
        public MotivoRechazo? Motivo { get; set; }
    }

    // Los cuatro estados que el contrato permite para la reserva de una solicitud.
    public enum EstadoReserva
    {
        Retenida,
        Confirmada,
        Rechazada,
        Vencida
    }

    // Los tres motivos de rechazo del contrato.
    public enum MotivoRechazo
    {
        AsientoOcupado,
        PagoFallido,
        PagoNoDisponible
    }
}