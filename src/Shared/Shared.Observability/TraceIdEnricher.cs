using Serilog.Core;
using Serilog.Events;

namespace Shared.Observability;

public sealed class TraceIdEnricher : ILogEventEnricher
{
    private readonly ITraceContext _traceContext;

    public TraceIdEnricher(ITraceContext traceContext)
    {
        _traceContext = traceContext;
    }

    public void Enrich( LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        if (_traceContext.TraceId is not null)
        {
            var traceIdProperty = propertyFactory.CreateProperty(
                "TraceId",
                _traceContext.TraceId);

            logEvent.AddPropertyIfAbsent(traceIdProperty);
        }
    }
}