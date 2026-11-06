namespace Availability.UseCases.Eventos.ListarEventos.Models
{
    // Datos de salida del caso de uso. Usa una clase propia y no la entidad Evento, igual que
    // ListarAsientosDisponibles, para que un cambio en el dominio no llegue al presenter.
    public class OutputDataResponse
    {
        public List<EventoResumen> Eventos { get; set; } = new();
    }

    // Lo que hace falta para listar un evento. No trae asientos porque el listado no los
    // muestra, y el repositorio tampoco los carga en ListarAsync.
    public class EventoResumen
    {
        // El Id viaja para armar el enlace a la página de asientos del evento.
        public Guid Id { get; set; }

        public string Nombre { get; set; } = string.Empty;

        public DateTime Fecha { get; set; }
    }
}
