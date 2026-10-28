// Tomás Di Loreto
using Availability.Entities.Eventos;
using Availability.UseCases.Eventos.ListarAsientosDisponibles.Interfaces;
using Availability.UseCases.Eventos.ListarAsientosDisponibles.Models;
using Availability.UseCases.Interfaces;

namespace Availability.UseCases.Eventos.ListarAsientosDisponibles
{
    // Interactor del caso de uso ListarAsientosDisponibles (paso 1 del enunciado).
    // Implementa el puerto de entrada, así el controller lo ejecuta a través de 
    // IInputPort sin conocer esta clase.

    // Recibe sus dependencias por constructor y todas son interfaces definidas en
    // este anillo. La clase concreta que finalmente llegue en cada una lo define
    // la configuración de la aplicación con program.cs
    public class Interactor : IInputPort
    {
        private readonly IEventoRepository _eventoRepository;
        private readonly IConsultaOcupacion _consultaOcupacion;
        private readonly IOutputPort _presenter;

        public Interactor(
            IEventoRepository eventoRepository,
            IConsultaOcupacion consultaOcupacion,
            IOutputPort presenter)
        {
            _eventoRepository = eventoRepository;
            _consultaOcupacion = consultaOcupacion;
            _presenter = presenter;
        }

        public async Task HandleAsync(InputDataRequest input)
        {
            // Pide el evento con sus asientos. Puede volver null si no existe.
            Evento? evento = await _eventoRepository.ObtenerPorIdAsync(input.EventoId);

            // Si el evento no existe, se informa y el caso de uso termina acá. No se consulta
            // a Booking, porque no hay asientos con los que cruzar la ocupación.
            if (evento is null)
            {
                await _presenter.HandleAsync(new OutputDataResponse
                {
                    EventoEncontrado = false,
                    EventoId = input.EventoId
                });
                return;
            }

            // Pide la ocupación del evento. Si booking no responde, no hay excepción, 
            // sino que vuelve con RespuestaOk en false, según IConsultaOcupacion.

            ResultadoOcupacion ocupacion = await _consultaOcupacion.ObtenerAsientosOcupadosAsync(evento.Id);
            // Diccionario de asientos ocupados: la clave es el Id del asiento y el valor,
            // su estado. Permite buscar cada asiento del evento sin recorrer toda la lista.
            
            var estadosOcupados = new Dictionary<Guid, EstadoOcupacion>();
            foreach (AsientoOcupado ocupado in ocupacion.AsientosOcupados)
            {
                estadosOcupados[ocupado.AsientoId] = ocupado.Estado;
            }

            //  Convierte cada asiento del evento en un AsientoDelEvento con su estado.
            List<AsientoDelEvento> asientos = evento.Asientos
                .Select(asiento => new AsientoDelEvento
                {
                    Id = asiento.Id,
                    Fila = asiento.Fila,
                    Numero = asiento.Numero,
                    Estado = CalcularEstado(asiento.Id, ocupacion.RespuestaOk, estadosOcupados)
                })
                // Orden por fila y número.
                .OrderBy(asiento => asiento.Fila.Length)
                .ThenBy(asiento => asiento.Fila, StringComparer.Ordinal)
                .ThenBy(asiento => asiento.Numero)
                .ToList();

            // Entrega el resultado
            await _presenter.HandleAsync(new OutputDataResponse
            {
                EventoEncontrado = true,
                EventoId = evento.Id,
                NombreEvento = evento.Nombre,
                Fecha = evento.Fecha,
                OcupacionDisponible = ocupacion.RespuestaOk,
                Asientos = asientos
            });
        }
        // Estado de un asiento para poder luego mostrar en pantalla
        private static EstadoAsiento CalcularEstado(
            Guid asientoId,
            bool respuestaOk,
            Dictionary<Guid, EstadoOcupacion> estadosOcupados)
        {
            if (!respuestaOk)
            {
                return EstadoAsiento.Desconocido;
            }

            if (estadosOcupados.TryGetValue(asientoId, out EstadoOcupacion estado))
            {
                return estado switch
                {
                    EstadoOcupacion.Retenida => EstadoAsiento.Pendiente,
                    EstadoOcupacion.Confirmada => EstadoAsiento.Reservado,
                    _ => throw new ArgumentOutOfRangeException(nameof(estado), estado, "Estado de ocupación no contemplado.")
                };
            }

            return EstadoAsiento.Disponible;
        }
    }
}

