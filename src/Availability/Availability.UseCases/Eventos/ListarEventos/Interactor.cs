using Availability.Entities.Eventos;
using Availability.UseCases.Eventos.ListarEventos.Interfaces;
using Availability.UseCases.Eventos.ListarEventos.Models;

namespace Availability.UseCases.Eventos.ListarEventos
{
    // Interactor del caso de uso ListarEventos, la página de inicio del sitio. Tiene la misma
    // forma que el de ListarAsientosDisponibles: recibe sus dependencias por constructor como
    // interfaces de este anillo y entrega el resultado por el puerto de salida.
    //
    // No consulta a Booking. El listado muestra qué eventos hay, y la ocupación se consulta
    // recién al entrar a un evento, así la página de inicio no depende de que Booking responda.
    public class Interactor : IInputPort
    {
        private readonly IEventoRepository _eventoRepository;
        private readonly IOutputPort _presenter;

        public Interactor(IEventoRepository eventoRepository, IOutputPort presenter)
        {
            _eventoRepository = eventoRepository;
            _presenter = presenter;
        }

        public async Task HandleAsync(InputDataRequest input)
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
        }
    }
}
