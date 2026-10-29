using Availability.Presenters.Comun;
using Availability.UseCases.Eventos.ListarEventos.Models;

namespace Availability.Presenters.ListarEventos
{
    // ViewModel de la página de inicio. Como en ListarAsientosViewModel, los textos y el
    // formato de las fechas se deciden acá, y la vista solo recorre la lista.
    public class ListarEventosViewModel
    {
        public ListarEventosViewModel(OutputDataResponse response)
        {
            Eventos = response.Eventos.Select(e => new EventoItemViewModel(e)).ToList();
        }

        public List<EventoItemViewModel> Eventos { get; internal set; }

        public bool HayEventos => Eventos.Count > 0;

        public string MensajeSinEventos { get; } = "Por ahora no hay eventos publicados.";
    }

    public class EventoItemViewModel
    {
        public EventoItemViewModel(EventoResumen evento)
        {
            EventoId = evento.Id;
            Nombre = evento.Nombre;
            Dia = FormatoFechas.Dia(evento.Fecha);
            Mes = FormatoFechas.MesAbreviado(evento.Fecha);
            DiaYHora = FormatoFechas.DiaYHora(evento.Fecha);
            FechaCompleta = FormatoFechas.Completa(evento.Fecha);
        }

        // Se usa solo para armar el enlace a los asientos; la vista no lo muestra como texto.
        public Guid EventoId { get; internal set; }

        public string Nombre { get; internal set; }

        // Día y mes por separado para mostrarlos como el bloque de calendario de la tarjeta.
        public string Dia { get; internal set; }

        public string Mes { get; internal set; }

        public string DiaYHora { get; internal set; }

        // La fecha completa va en el texto para lectores de pantalla, que no interpretan el
        // bloque de día y mes como una fecha.
        public string FechaCompleta { get; internal set; }
    }
}
