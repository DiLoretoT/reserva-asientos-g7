using Microsoft.AspNetCore.Builder;

namespace Shared.Observability;

public static class TraceIdMiddlewareExtensions{
    public static IApplicationBuilder UseTraceId(this IApplicationBuilder app){
        return app.UseMiddleware<TraceIdMiddleware>();
    }
}
