using Grpc.Core;
using Grpc.Core.Interceptors;

namespace Shared.Observability;

public sealed class TraceIdGrpcClientInterceptor : Interceptor{
    private const string MetadataKey = "x-trace-id";
    private readonly ITraceContext _traceContext;

    public TraceIdGrpcClientInterceptor(ITraceContext traceContext){
        _traceContext = traceContext;
    }

    public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
        TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncUnaryCallContinuation<TRequest, TResponse> continuation){

        var traceId = _traceContext.TraceId;

        if (traceId is not null){
            var headers = new Metadata();

            if (context.Options.Headers is not null){
                foreach (var entrada in context.Options.Headers){
                    headers.Add(entrada);
                }
            }

            if (headers.Get(MetadataKey) is null){
                headers.Add(MetadataKey, traceId);
            }

            var opciones = context.Options.WithHeaders(headers);

            context = new ClientInterceptorContext<TRequest, TResponse>(
                context.Method,
                context.Host,
                opciones);
        }

        return continuation(request, context);
    }
}