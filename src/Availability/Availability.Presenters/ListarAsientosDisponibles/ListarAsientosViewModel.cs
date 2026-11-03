// Tomás Di Loreto
using Availability.Presenters.Comun;
using Availability.UseCases.Eventos.ListarAsientosDisponibles.Models;

namespace Availability.Presenters.ListarAsientosDisponibles
{
    // ViewModel de la página de asientos de un evento. El presenter lo arma a partir del
    // OutputDataResponse y la vista solo lo recorre, así los textos, los colores, el formato
    // de la fecha y la agrupación por fila se deciden en el anillo 3 y no en Razor.
    // El visitante identifica un asiento por fila y número, y esos son los únicos datos que
    // se le muestran. Los Id del evento y de cada asiento viajan como datos ocultos del
    // formulario de reserva (issue #4), porque es lo que necesita el caso de uso
    // SolicitarReserva para registrar la solicitud.
    public class ListarAsientosViewModel
    {
        public ListarAsientosViewModel(OutputDataResponse response)
        {
            EventoEncontrado = response.EventoEncontrado;
            EventoId = response.EventoId;

            if (!response.EventoEncontrado)
            {
                MensajeEventoNoEncontrado =
                    "No encontramos el evento que buscás. Revisá que el enlace sea correcto.";
                return;
            }

            NombreEvento = response.NombreEvento;
            FechaTexto = FormatoFechas.Completa(response.Fecha);

            if (!response.OcupacionDisponible)
            {
                AvisoOcupacionTitulo = "No pudimos confirmar la disponibilidad";
                AvisoOcupacion =
                    "En este momento no podemos saber qué asientos están libres, así que los " +
                    "mostramos sin estado. Volvé a intentar en unos minutos.";
            }

            // El Interactor entrega los asientos ordenados por fila y número, y GroupBy
            // respeta ese orden, así que el presenter no vuelve a ordenar.
            Filas = response.Asientos
                .GroupBy(a => a.Fila)
                .Select(grupo => new FilaViewModel
                {
                    Letra = grupo.Key,
                    Nombre = $"Fila {grupo.Key}",
                    Asientos = grupo.Select(a => new AsientoViewModel(a)).ToList()
                })
                .ToList();
        }

        public bool EventoEncontrado { get; internal set; }

        // Dato oculto del formulario de reserva.
        public Guid EventoId { get; internal set; }

        public string? MensajeEventoNoEncontrado { get; internal set; }

        public string NombreEvento { get; internal set; } = string.Empty;

        public string FechaTexto { get; internal set; } = string.Empty;

        public string? AvisoOcupacionTitulo { get; internal set; }

        public string? AvisoOcupacion { get; internal set; }

        public List<FilaViewModel> Filas { get; internal set; } = new();

        // La referencia de colores sale de la misma tabla que usan los asientos
        // (AsientoViewModel.Presentacion), así la vista no repite clases ni textos y la
        // referencia no puede quedar distinta de lo que muestra el mapa.
        public List<ReferenciaViewModel> Referencias { get; } =
            Enum.GetValues<EstadoAsiento>()
                .Select(AsientoViewModel.Presentacion)
                .Select(p => new ReferenciaViewModel { Texto = p.Texto, ClaseCss = p.ClaseCss })
                .ToList();
    }

    public class FilaViewModel
    {
        // La letra va sola al costado del mapa; el nombre completo, para lectores de pantalla.
        public string Letra { get; internal set; } = string.Empty;

        public string Nombre { get; internal set; } = string.Empty;

        public List<AsientoViewModel> Asientos { get; internal set; } = new();
    }

    public class AsientoViewModel
    {
        public AsientoViewModel(AsientoDelEvento asiento)
        {
            Id = asiento.Id;
            Etiqueta = $"{asiento.Fila}{asiento.Numero}";
            Numero = asiento.Numero.ToString();
            (EstadoTexto, ClaseCss) = Presentacion(asiento.Estado);
            Seleccionable = asiento.Estado == EstadoAsiento.Disponible;

            // El estado no puede depender solo del color. Este texto es el que leen los
            // lectores de pantalla y el que aparece al pasar el mouse o al enfocar el asiento.
            Descripcion = $"Fila {asiento.Fila}, asiento {asiento.Numero}: {EstadoTexto.ToLowerInvariant()}";
        }

        // No se muestra: la vista lo deja en el botón del asiento para que, al elegirlo, el
        // formulario de reserva envíe qué asiento se pidió.
        public Guid Id { get; internal set; }

        // Fila y número juntos, como figura el asiento en una entrada, por ejemplo A2.
        public string Etiqueta { get; internal set; }

        // En el mapa la letra de la fila ya está al costado, así que el botón muestra solo
        // el número.
        public string Numero { get; internal set; }

        public string EstadoTexto { get; internal set; }

        public string ClaseCss { get; internal set; }

        public string Descripcion { get; internal set; }

        // Solo un asiento disponible se puede elegir. Lo decide el presenter y no el
        // JavaScript de la página, que solo lee este dato.
        public bool Seleccionable { get; internal set; }

        // Texto y clase de cada estado. Las clases son las del CSS propio del sitio
        // (wwwroot/css/site.css); cada estado se distingue también por la forma (relleno,
        // rayado, borde punteado) y no solo por el color.
        internal static (string Texto, string ClaseCss) Presentacion(EstadoAsiento estado) =>
            estado switch
            {
                EstadoAsiento.Disponible => ("Disponible", "asiento--disponible"),
                EstadoAsiento.Pendiente => ("Pendiente de pago", "asiento--pendiente"),
                EstadoAsiento.Reservado => ("Reservado", "asiento--reservado"),
                EstadoAsiento.Desconocido => ("Sin información", "asiento--sin-info"),
                _ => throw new ArgumentOutOfRangeException(nameof(estado), estado, "Estado de asiento no contemplado.")
            };
    }

    public class ReferenciaViewModel
    {
        public string Texto { get; internal set; } = string.Empty;

        public string ClaseCss { get; internal set; } = string.Empty;
    }
}
