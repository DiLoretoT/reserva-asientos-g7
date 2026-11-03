using Azure.Messaging.ServiceBus;
using Booking.Business;
using Microsoft.Extensions.Options;

namespace Booking.Api.Broker;

public static class ServiceBusExtensions
{
    public static IServiceCollection AddBookingServiceBus(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<ServiceBusOptions>()
            .Bind(configuration.GetSection(ServiceBusOptions.SectionName))
            .Validate(o => !o.Enabled || ConexionValida(o.ConnectionString),
                "ServiceBus:ConnectionString es obligatoria y debe ser válida si se habilita el consumidor.")
            .Validate(o => !o.Enabled || (!string.IsNullOrWhiteSpace(o.TopicName)
                && !string.IsNullOrWhiteSpace(o.SubscriptionName)),
                "ServiceBus:TopicName y ServiceBus:SubscriptionName son obligatorios al habilitar el consumidor.")
            .ValidateOnStart();

        services.AddScoped<ProcesamientoSolicitudReserva>();
        if (!configuration.GetValue<bool>("ServiceBus:Enabled"))
            return services;

        services.AddSingleton(sp => new ServiceBusClient(
            sp.GetRequiredService<IOptions<ServiceBusOptions>>().Value.ConnectionString));
        services.AddSingleton(sp =>
        {
            var opciones = sp.GetRequiredService<IOptions<ServiceBusOptions>>().Value;
            return sp.GetRequiredService<ServiceBusClient>().CreateProcessor(
                opciones.TopicName, opciones.SubscriptionName, CrearOpcionesProcessor());
        });
        services.AddSingleton<ManejadorSolicitudServiceBus>();
        services.AddHostedService<ConsumidorSolicitudesServiceBus>();
        return services;
    }

    public static ServiceBusProcessorOptions CrearOpcionesProcessor() => new()
    {
        ReceiveMode = ServiceBusReceiveMode.PeekLock,
        AutoCompleteMessages = false,
        PrefetchCount = 0,
        MaxConcurrentCalls = 1,
        MaxAutoLockRenewalDuration = TimeSpan.FromMinutes(5)
    };

    private static bool ConexionValida(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return false;
        try
        {
            ServiceBusConnectionStringProperties.Parse(connectionString);
            return true;
        }
        catch (Exception ex) when (ex is ArgumentException or FormatException)
        {
            return false;
        }
    }
}
