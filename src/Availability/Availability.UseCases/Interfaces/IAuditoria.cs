// Matías De La Rosa
namespace Availability.UseCases.Interfaces
{
    // Puerto de salida para los logs de auditoría (punto f del enunciado, issue #9): cada caso
    // de uso registra el inicio y el fin de su cadena de sucesos, según contracts/logs.md.
    //
    // Los casos de uso no usan Shared.Observability directamente porque ese proyecto depende de
    // ASP.NET Core (Serilog, el middleware de TraceId). Si este anillo lo referenciara, el
    // centro de la arquitectura quedaría atado al framework web, y se rompería la regla de
    // dependencia de Clean Architecture: UseCases solo conoce a Entities. Por eso el anillo
    // define este puerto con lo que necesita, y el adaptador de Availability.Gateways lo
    // implementa con IAuditLogger e ITraceContext. Es el mismo patrón que IPublicadorSolicitudes
    // o IConsultaOcupacion: si mañana se cambia Serilog por otra herramienta, cambia el
    // adaptador y los casos de uso no se tocan.
    public interface IAuditoria
    {
        // Usa traceId como TraceId de todo lo que se registre a continuación en esta operación,
        // incluidas las llamadas a Booking. Lo usan los casos de uso cuyo TraceId, según el
        // contrato, es el solicitudId (SolicitarReserva y ConsultarEstadoSolicitud). Los demás no
        // lo llaman y usan el TraceId que el middleware generó para el pedido.
        void AsociarTraza(Guid traceId);

        // Registra "[Inicio Operación]". operacion va en snake_case, por ejemplo
        // "solicitar_reserva", como pide el contrato.
        void Inicio(string operacion);

        // Registra "[Fin Operación]" con su resultado. El motivo es opcional y explica un
        // resultado rechazada o error, por ejemplo "asiento_inexistente".
        void Fin(string operacion, ResultadoAuditoria resultado, string? motivo = null);
    }

    // Los tres resultados de la sección 4 de contracts/logs.md. Es un enum propio del anillo y
    // no el texto del contrato ("ok", "rechazada", "error"): la traducción al texto la hace el
    // adaptador, así un cambio en el formato del log no llega a los casos de uso.
    public enum ResultadoAuditoria
    {
        // La operación terminó como se esperaba.
        Ok,

        // La operación terminó por una regla del sistema, por ejemplo un asiento inexistente.
        Rechazada,

        // La operación no pudo completarse por una falla, por ejemplo el broker caído o una
        // excepción.
        Error
    }
}
