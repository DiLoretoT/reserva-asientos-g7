using Microsoft.Extensions.Logging;

namespace Shared.Observability;

public interface IAuditLogger
{
    void Inicio(string operacion);
    void Fin(string operacion, string resultado);
}

public sealed class AuditLogger : IAuditLogger
{
    private readonly ILogger<AuditLogger> _logger;

    public AuditLogger(ILogger<AuditLogger> logger)
    {
        _logger = logger;
    }

    public void Inicio(string operacion)
    {
        _logger.LogInformation("[Inicio Operación] {Operacion}", operacion);
    }

    public void Fin(string operacion, string resultado)
    {
        _logger.LogInformation("[Fin Operación] {Operacion} {Resultado}", operacion, resultado);
    }
}