using System.Net.Sockets;
using Grpc.Core;
using Microsoft.Extensions.Options;
using Payment.Grpc;

namespace Booking.Data.Payment;

public class ClientePayment(
    PaymentGrpc.PaymentGrpcClient client,
    IOptions<PaymentOptions> options,
    Func<CancellationToken, Task> obtenerToken) : IClientePayment
{
    public async Task<ResultadoPago> CobrarAsync(Guid reservaId, decimal monto, CancellationToken cancellationToken)
    {
        var timeout = TimeSpan.FromSeconds(options.Value.DeadlineSeconds);
        var deadline = DateTime.UtcNow.Add(timeout);
        using var tokenTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        tokenTimeout.CancelAfter(timeout);
        try
        {
            // Separa un fallo previo al envío de los errores inciertos que gRPC puede envolver.
            await obtenerToken(tokenTimeout.Token);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception) { return ResultadoPago.NoDisponible; }

        try
        {
            var response = await client.ProcessPaymentAsync(new ProcessPaymentRequest
            {
                BookingId = reservaId.ToString("D"),
                Amount = (double)monto
            }, deadline: deadline, cancellationToken: cancellationToken);

            if (!Guid.TryParse(response.BookingId, out var recibido) || recibido != reservaId)
                return ResultadoPago.Incierto;

            return response.PaymentStatus switch
            {
                "Approved" => ResultadoPago.Aprobado,
                "Rejected" => ResultadoPago.Rechazado,
                _ => ResultadoPago.Incierto
            };
        }
        catch (RpcException) when (cancellationToken.IsCancellationRequested)
        {
            cancellationToken.ThrowIfCancellationRequested();
            throw;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Unavailable && ConexionRechazada(ex.Status.DebugException))
        {
            return ResultadoPago.NoDisponible;
        }
        catch (Exception) { return ResultadoPago.Incierto; }
    }

    private static bool ConexionRechazada(Exception? error)
        => error is SocketException { SocketErrorCode: SocketError.ConnectionRefused }
            || (error?.InnerException is { } inner && ConexionRechazada(inner));
}
