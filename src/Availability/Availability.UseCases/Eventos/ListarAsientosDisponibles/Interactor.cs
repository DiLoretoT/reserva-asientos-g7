// Tomás Di Loreto
using Availability.Entities.Eventos;
using Availability.UseCases.Eventos;
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
    //
    // Auditoría (issue #9, contracts/logs.md, Matías De La Rosa): operación "listar_asientos".
    // Usa el TraceId que el middleware generó para el pedido; el cliente de Booking lo manda en
    // el header X-Trace-Id, así la consulta de ocupación queda con el mismo TraceId en los logs
    // de Booking.
    public class Interactor : IInputPort
    {
        private const string Operacion = "listar_asientos";

        private readonly IEventoRepository _eventoRepository;
        private readonly IConsultaOcupacion _consultaOcupacion;
        private readonly IOutputPort _presenter;
        private readonly IAuditoria _auditoria;

        public Interactor(
            IEventoRepository eventoRepository,
            IConsultaOcupacion consultaOcupacion,
            IOutputPort presenter,
            IAuditoria auditoria)
        {
            _eventoRepository = eventoRepository;
            _consultaOcupacion = consultaOcupacion;
            _presenter = presenter;
            _auditoria = auditoria;
        }

        public async Task HandleAsync(InputDataRequest input)
        {
            _auditoria.Inicio(Operacion);

            // Arranca en Error: si algo lanza una excepción, el finally registra el fin igual.
            ResultadoAuditoria resultado = ResultadoAuditoria.Error;
            string? motivo = "excepcion";

            try
            {
                (resultado, motivo) = await ListarAsync(input);
            }
            finally
            {
                _auditoria.Fin(Operacion, resultado, motivo);
            }
        }

        // El trabajo del caso de uso. Devuelve cómo terminó, para que HandleAsync lo audite.
        private async Task<(ResultadoAuditoria Resultado, string? Motivo)> ListarAsync(InputDataRequest input)
        {
            // Pide el evento con sus asientos. Puede volver null si no existe.
            Evento? evento = await _eventoRepository.ObtenerConAsientosAsync(input.EventoId);

            // Si el evento no existe, se informa y el caso de uso termina acá. No se consulta
            // a Booking, porque no hay asientos con los que cruzar la ocupación.
            if (evento is null)
            {
                await _presenter.HandleAsync(new OutputDataResponse
                {
                    EventoEncontrado = false,
                    EventoId = input.EventoId
                });
                return (ResultadoAuditoria.Rechazada, "evento_inexistente");
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

            // Si Booking no respondió, la operación igual termina bien: la página se muestra con
            // el aviso y los asientos sin estado (se prioriza la disponibilidad). Por eso el
            // resultado es Ok, y el motivo deja registrado que faltó la ocupación.
            return ocupacion.RespuestaOk
                ? (ResultadoAuditoria.Ok, null)
                : (ResultadoAuditoria.Ok, "ocupacion_no_disponible");
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

