using Grpc.Core;
using Grpc.Core.Interceptors;

namespace Shared.Observability;

public sealed class TraceIdGrpcInterceptor : Interceptor{
    private const string MetadataKey = "x-trace-id";

    private readonly ITraceContext _traceContext;

    public TraceIdGrpcInterceptor(ITraceContext traceContext){
        _traceContext = traceContext;
    }

    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(TRequest request, ServerCallContext context, UnaryServerMethod<TRequest, TResponse> continuation){
        var traceId = context.RequestHeaders.GetValue(MetadataKey);

        if (Guid.TryParse(traceId, out var guid)){
            _traceContext.TraceId = guid.ToString();
        }else{
            _traceContext.TraceId = Guid.NewGuid().ToString();
        }

        try{
            return await continuation(request, context);
        }finally{
            _traceContext.TraceId = null;
        }
    }
}
