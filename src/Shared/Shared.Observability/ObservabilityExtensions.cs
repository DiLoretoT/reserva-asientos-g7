using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Shared.Observability;

public static class ObservabilityExtensions
{
    public static WebApplicationBuilder AddObservability(this WebApplicationBuilder builder, string servicio)
    {
        builder.Services.AddSingleton<ITraceContext, TraceContext>();
        builder.Services.AddSingleton<IAuditLogger, AuditLogger>();
        builder.Services.AddSingleton<TraceIdEnricher>();

        var seqUrl = builder.Configuration["Seq:ServerUrl"]
            ?? "http://localhost:5341";

        builder.Host.UseSerilog((contexto, servicios, configuracion) => configuracion
            .MinimumLevel.Information()
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Servicio", servicio)
            .Enrich.With(servicios.GetRequiredService<TraceIdEnricher>())
            .WriteTo.Console()
            .WriteTo.Seq(seqUrl));

        return builder;
    }
}
