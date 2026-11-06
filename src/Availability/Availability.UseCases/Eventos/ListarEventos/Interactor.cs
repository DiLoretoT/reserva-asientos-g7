using Availability.Entities.Eventos;
using Availability.UseCases.Eventos.ListarEventos.Interfaces;
using Availability.UseCases.Eventos.ListarEventos.Models;
using Availability.UseCases.Interfaces;

namespace Availability.UseCases.Eventos.ListarEventos
{
    // Interactor del caso de uso ListarEventos, la página de inicio del sitio. Tiene la misma
    // forma que el de ListarAsientosDisponibles: recibe sus dependencias por constructor como
    // interfaces de este anillo y entrega el resultado por el puerto de salida.
    //
    // No consulta a Booking. El listado muestra qué eventos hay, y la ocupación se consulta
    // recién al entrar a un evento, así la página de inicio no depende de que Booking responda.
    //
    // Auditoría (issue #9, contracts/logs.md): operación "listar_eventos". Usa el TraceId que el
    // middleware generó para el pedido, porque no nace de una solicitud de reserva.
    public class Interactor : IInputPort
    {
        private const string Operacion = "listar_eventos";

        private readonly IEventoRepository _eventoRepository;
        private readonly IOutputPort _presenter;
        private readonly IAuditoria _auditoria;

        public Interactor(IEventoRepository eventoRepository, IOutputPort presenter, IAuditoria auditoria)
        {
            _eventoRepository = eventoRepository;
            _presenter = presenter;
            _auditoria = auditoria;
        }

        public async Task HandleAsync(InputDataRequest input)
        {
            _auditoria.Inicio(Operacion);

            // El resultado arranca en Error y se cambia recién al terminar bien. Si algo lanza una
            // excepción, el finally registra el fin con Error, como pide el contrato ("se registra
            // también cuando la operación falla"), y la excepción sigue su camino.
            ResultadoAuditoria resultado = ResultadoAuditoria.Error;
            string? motivo = "excepcion";

            try
            {
                List<Evento> eventos = await _eventoRepository.ListarAsync();

                // El orden por fecha es una regla del caso de uso (primero el evento más próximo),
                // así que se aplica acá aunque EventoRepository ya ordene la consulta. De esa forma
                // el resultado no depende de cómo esté implementado el repositorio.
                await _presenter.HandleAsync(new OutputDataResponse
                {
                    Eventos = eventos
                        .OrderBy(e => e.Fecha)
                        .Select(e => new EventoResumen
                        {
                            Id = e.Id,
                            Nombre = e.Nombre,
                            Fecha = e.Fecha
                        })
                        .ToList()
                });

                resultado = ResultadoAuditoria.Ok;
                motivo = null;
            }
            finally
            {
                _auditoria.Fin(Operacion, resultado, motivo);
            }
        }
    }
}
