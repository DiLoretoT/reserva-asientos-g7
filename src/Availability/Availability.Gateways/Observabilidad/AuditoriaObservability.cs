// Matías De La Rosa
using Availability.UseCases.Interfaces;
using Serilog.Context;
using Shared.Observability;

namespace Availability.Gateways.Observabilidad
{
    // Adaptador del puerto IAuditoria (Ports & Adapters) con Shared.Observability, el proyecto
    // común de logs del grupo. Vive en Gateways, el anillo de los detalles técnicos, que sí puede
    // depender de Serilog y de ASP.NET Core. Los casos de uso solo conocen IAuditoria.
    public class AuditoriaObservability : IAuditoria
    {
        private readonly IAuditLogger _auditLogger;
        private readonly ITraceContext _traceContext;

        public AuditoriaObservability(IAuditLogger auditLogger, ITraceContext traceContext)
        {
            _auditLogger = auditLogger;
            _traceContext = traceContext;
        }

        public void AsociarTraza(Guid traceId)
        {
            // ITraceContext guarda el TraceId en un AsyncLocal, un valor que acompaña al flujo
            // asincrónico actual. El cambio vale para todo lo que el caso de uso hace después,
            // incluidos sus logs y las llamadas a Booking (TraceIdHttpClientHandler lee este
            // mismo valor), y no afecta a otros pedidos que corren al mismo tiempo.
            _traceContext.TraceId = traceId.ToString();
        }

        public void Inicio(string operacion)
        {
            _auditLogger.Inicio(operacion);
        }

        public void Fin(string operacion, ResultadoAuditoria resultado, string? motivo = null)
        {
            string resultadoContrato = AResultadoContrato(resultado);

            if (motivo is null)
            {
                _auditLogger.Fin(operacion, resultadoContrato);
                return;
            }

            // IAuditLogger.Fin no recibe el motivo. Se agrega como propiedad "Motivo" del log con
            // el contexto de Serilog (AddObservability activa Enrich.FromLogContext), así aparece
            // en Seq junto a la línea de fin sin cambiar el código común del grupo. La propiedad
            // se quita al salir del using y no se mezcla con otros logs.
            using (LogContext.PushProperty("Motivo", motivo))
            {
                _auditLogger.Fin(operacion, resultadoContrato);
            }
        }

        // Los textos de la sección 4 de contracts/logs.md.
        private static string AResultadoContrato(ResultadoAuditoria resultado) =>
            resultado switch
            {
                ResultadoAuditoria.Ok => "ok",
                ResultadoAuditoria.Rechazada => "rechazada",
                ResultadoAuditoria.Error => "error",
                _ => throw new ArgumentOutOfRangeException(nameof(resultado), resultado, "Resultado de auditoría no contemplado.")
            };
    }
}
