using System.Data.Common;
using Azure.Messaging.ServiceBus;
using Booking.Business;
using Booking.Domain;
using Serilog.Context;
using Shared.Observability;

namespace Booking.Api.Broker;

public class ManejadorSolicitudServiceBus(
    IServiceScopeFactory scopes,
    ITraceContext trace,
    IAuditLogger auditoria,
    ILogger<ManejadorSolicitudServiceBus> logger)
{
    private const string Operacion = "procesar_reserva";
    private readonly TaskCompletionSource<Guid> pagoPendiente = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public Task<Guid> PagoPendiente => pagoPendiente.Task;

    public async Task ProcesarAsync(ProcessMessageEventArgs args, CancellationToken stoppingToken)
    {
        // StopProcessingAsync es externo al callback; puede llegar otra entrega antes de que termine.
        if (pagoPendiente.Task.IsCompleted)
            return;

        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(
            args.CancellationToken, stoppingToken);
        var token = cancellation.Token;
        var traceAnterior = trace.TraceId;
        trace.TraceId = Guid.NewGuid().ToString("D");
        var resultado = "error";
        var motivo = "excepcion";
        var duplicado = false;
        var iniciada = false;
        try
        {
            token.ThrowIfCancellationRequested();
            var validacion = ValidadorSolicitudServiceBus.Validar(args.Message);
            if (validacion.TraceId is { } solicitudId)
                trace.TraceId = solicitudId.ToString("D");
            auditoria.Inicio(Operacion);
            iniciada = true;
            if (validacion.MotivoDlq is { } error)
            {
                await EnviarDlqAsync(args, error, token);
                resultado = "rechazada";
                motivo = error;
                return;
            }

            await using var scope = scopes.CreateAsyncScope();
            var servicio = scope.ServiceProvider.GetRequiredService<ProcesamientoSolicitudReserva>();
            var decision = await servicio.ProcesarAsync(validacion.Solicitud!, token);
            duplicado = decision.EsDuplicado;
            switch (decision.Estado)
            {
                case EstadoProcesamientoReserva.Conflicto:
                    await EnviarDlqAsync(args, "campo_invalido", token);
                    resultado = "rechazada";
                    motivo = "campo_invalido";
                    break;
                case EstadoProcesamientoReserva.PendientePago:
                    // No perder la solicitud ni agotar reentregas mientras Payment no esté integrado.
                    motivo = "pago_pendiente";
                    pagoPendiente.TrySetResult(validacion.Solicitud!.SolicitudId);
                    break;
                case EstadoProcesamientoReserva.Finalizada:
                case EstadoProcesamientoReserva.Rechazada:
                    await args.CompleteMessageAsync(args.Message, token);
                    resultado = decision.Estado == EstadoProcesamientoReserva.Rechazada ? "rechazada" : "ok";
                    motivo = decision.Motivo switch
                    {
                        MotivoRechazo.AsientoOcupado => "asiento_ocupado",
                        MotivoRechazo.PagoFallido => "pago_fallido",
                        MotivoRechazo.PagoNoDisponible => "pago_no_disponible",
                        _ => "procesada"
                    };
                    break;
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            motivo = "cancelada";
        }
        catch (Exception ex)
        {
            var transitorio = EsTransitorio(ex);
            motivo = transitorio ? "fallo_transitorio" : "fallo_procesamiento";
            // No registrar la excepción original: puede incluir datos del cuerpo o de la conexión.
            logger.Log(transitorio ? LogLevel.Warning : LogLevel.Error,
                "Fallo de {Operacion}: {Motivo}, tipo {TipoError}. {Resultado}",
                Operacion, motivo, ex.GetType().Name, "error");
            if (transitorio && !token.IsCancellationRequested)
            {
                try { await args.AbandonMessageAsync(args.Message, cancellationToken: token); }
                catch (Exception)
                {
                    // Si se perdió el lock o la conexión, Azure podrá reentregar sin confirmación.
                    motivo = "reentrega_pendiente";
                }
            }
        }
        finally
        {
            try
            {
                using var propiedadMotivo = LogContext.PushProperty("Motivo", motivo);
                using var propiedadDuplicado = LogContext.PushProperty("Duplicado", duplicado);
                if (iniciada)
                    auditoria.Fin(Operacion, resultado);
            }
            finally { trace.TraceId = traceAnterior; }
        }
    }

    private static Task EnviarDlqAsync(ProcessMessageEventArgs args, string motivo, CancellationToken token)
        => args.DeadLetterMessageAsync(args.Message, motivo,
            "El mensaje no cumple el contrato de solicitud de reserva.", token);

    private static bool EsTransitorio(Exception ex)
        => ex is TimeoutException or DbException { IsTransient: true }
            or ServiceBusException { IsTransient: true }
            || (ex.InnerException is { } inner && EsTransitorio(inner));
}
