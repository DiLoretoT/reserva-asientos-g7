using Microsoft.AspNetCore.Http;

namespace Shared.Observability;

public sealed class TraceIdMiddleware{
    public const string HeaderName = "X-Trace-Id";

    private readonly RequestDelegate _next;
    private readonly ITraceContext _traceContext;

    public TraceIdMiddleware(RequestDelegate next, ITraceContext traceContext){
        _next = next;
        _traceContext = traceContext;
    }

    public async Task InvokeAsync(HttpContext context){
        var traceId = ObtenerOGenerarTraceId(context);

        _traceContext.TraceId = traceId;
        context.Response.Headers[HeaderName] = traceId;

        try {
            await _next(context);
        } finally {
            _traceContext.TraceId = null;
        }
    }

    private static string ObtenerOGenerarTraceId(HttpContext context){
        var recibido = context.Request.Headers[HeaderName].FirstOrDefault();

        if (Guid.TryParse(recibido, out var guid))
            return guid.ToString();

        return Guid.NewGuid().ToString();
    }
}