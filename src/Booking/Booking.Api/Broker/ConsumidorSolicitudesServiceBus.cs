using Azure.Messaging.ServiceBus;
using Shared.Observability;

namespace Booking.Api.Broker;

public class ConsumidorSolicitudesServiceBus(
    ServiceBusProcessor processor,
    ManejadorSolicitudServiceBus manejador,
    ITraceContext trace,
    ILogger<ConsumidorSolicitudesServiceBus> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        async Task Procesar(ProcessMessageEventArgs args)
        {
            try { await manejador.ProcesarAsync(args, stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
            catch (Exception ex)
            {
                // Una excepción que escape del callback causa Abandon automático incluso con AutoComplete=false.
                RegistrarError(ex, "callback");
            }
        }
        processor.ProcessMessageAsync += Procesar;
        processor.ProcessErrorAsync += ProcesarErrorAsync;
        try
        {
            await processor.StartProcessingAsync(stoppingToken);
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
        catch (Exception ex) { RegistrarError(ex, "inicio"); }
        finally
        {
            // Se detiene fuera del callback para no esperar al propio mensaje.
            try { await processor.StopProcessingAsync(CancellationToken.None); }
            catch (Exception ex) { RegistrarError(ex, "detencion"); }
            finally
            {
                if (!processor.IsProcessing)
                {
                    processor.ProcessMessageAsync -= Procesar;
                    processor.ProcessErrorAsync -= ProcesarErrorAsync;
                }
            }
        }
    }

    private Task ProcesarErrorAsync(ProcessErrorEventArgs args)
    {
        RegistrarError(args.Exception, args.ErrorSource.ToString());
        return Task.CompletedTask;
    }

    private void RegistrarError(Exception exception, string origen)
    {
        var anterior = trace.TraceId;
        trace.TraceId = Guid.NewGuid().ToString("D");
        try
        {
            logger.LogError("Error de Service Bus: {TipoError}, origen {Origen}. {Operacion} {Resultado}",
                exception.GetType().Name, origen, "consumir_solicitudes", "error");
        }
        finally { trace.TraceId = anterior; }
    }
}
