using System.Data.Common;
using Booking.Business;
using Microsoft.Extensions.Options;

namespace Booking.Api;

public class VencimientoReservasService(
    IServiceScopeFactory scopeFactory,
    IOptions<BookingOptions> opciones) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(
            TimeSpan.FromSeconds(opciones.Value.ExpirationIntervalSeconds));

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await using var scope = scopeFactory.CreateAsyncScope();
                    await scope.ServiceProvider.GetRequiredService<VencimientoReservas>()
                        .VencerAsync(stoppingToken);
                }
                catch (Exception ex) when (ex is DbException { IsTransient: true }
                    or TimeoutException
                    or InvalidOperationException { InnerException: DbException { IsTransient: true } })
                {
                    // Un fallo transitorio de la base se reintenta en el siguiente ciclo.
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }
}
