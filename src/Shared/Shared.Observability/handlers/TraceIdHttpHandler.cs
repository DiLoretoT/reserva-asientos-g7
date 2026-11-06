namespace Shared.Observability;

public sealed class TraceIdHttpClientHandler : DelegatingHandler{
    private readonly ITraceContext _traceContext;

    public TraceIdHttpClientHandler(ITraceContext traceContext){
        _traceContext = traceContext;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken){
        var traceId = _traceContext.TraceId;

        if (traceId is not null && !request.Headers.Contains(TraceIdMiddleware.HeaderName)){
            request.Headers.Add(TraceIdMiddleware.HeaderName, traceId);
        }

        return base.SendAsync(request, cancellationToken);
    }
}