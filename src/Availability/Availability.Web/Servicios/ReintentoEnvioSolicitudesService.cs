using ReintentarEnvio = Availability.UseCases.Solicitudes.ReintentarEnvio;

namespace Availability.Web.Servicios
{
    // Servicio en segundo plano del patrón Outbox. SolicitarReserva guarda siempre la solicitud
    // en la base de Availability y, si el broker no responde, la deja pendiente de envío. Este
    // servicio ejecuta cada 15 segundos el caso de uso ReintentarEnvio, que vuelve a publicar
    // las pendientes con el mismo solicitudId (el MessageId del contrato del broker) y las marca
    // como enviadas cuando el broker confirma. Así una caída del broker demora las solicitudes
    // pero no las pierde, y el usuario no tiene que volver a pedir el asiento.
    //
    // Vive en Availability.Web, el anillo 4, porque correr una tarea periódica dentro del
    // proceso es un mecanismo de ASP.NET Core (BackgroundService). La regla de qué se reenvía y
    // cuándo una solicitud queda enviada está en el caso de uso, en el anillo 2. Este servicio
    // lo ejecuta por su IInputPort, igual que un controller ejecuta el suyo: decide cuándo
    // correrlo, no qué hace.
    public class ReintentoEnvioSolicitudesService : BackgroundService
    {
        private static readonly TimeSpan Intervalo = TimeSpan.FromSeconds(15);

        private readonly IServiceScopeFactory _scopes;
        private readonly ILogger<ReintentoEnvioSolicitudesService> _logger;

        // Recibe la fábrica de scopes y no el caso de uso: el servicio vive mientras viva la
        // aplicación (Singleton), pero el Interactor depende de los repositorios y del DbContext,
        // que son Scoped. Cada vuelta abre su propio scope, como si fuera un pedido HTTP nuevo, y
        // lo cierra al terminar, así el DbContext no queda abierto entre vueltas.
        public ReintentoEnvioSolicitudesService(IServiceScopeFactory scopes, ILogger<ReintentoEnvioSolicitudesService> logger)
        {
            _scopes = scopes;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // PeriodicTimer espera el intervalo entre vueltas sin bloquear un hilo, y deja de
            // esperar cuando la aplicación se detiene (stoppingToken).
            using var timer = new PeriodicTimer(Intervalo);

            try
            {
                while (await timer.WaitForNextTickAsync(stoppingToken))
                {
                    await EjecutarVueltaAsync();
                }
            }
            catch (OperationCanceledException)
            {
                // La aplicación se está deteniendo: no es un error.
            }
        }

        private async Task EjecutarVueltaAsync()
        {
            try
            {
                using IServiceScope scope = _scopes.CreateScope();
                var reintentarEnvio = scope.ServiceProvider.GetRequiredService<ReintentarEnvio.Interfaces.IInputPort>();
                await reintentarEnvio.HandleAsync(new ReintentarEnvio.Models.InputDataRequest());
            }
            catch (Exception ex)
            {
                // Una falla inesperada (por ejemplo, la base de Availability caída al leer las
                // pendientes) no puede detener el servicio: se registra y se vuelve a intentar en
                // la próxima vuelta. Las fallas al reenviar una solicitud ya quedan en su propia
                // auditoría y no llegan acá.
                _logger.LogError(ex, "Falló una vuelta del reintento de envío de solicitudes.");
            }
        }
    }
}
