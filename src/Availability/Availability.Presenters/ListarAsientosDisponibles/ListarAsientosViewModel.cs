// Tomás Di Loreto
using System.Globalization;
using Availability.UseCases.Eventos.ListarAsientosDisponibles.Models;

namespace Availability.Presenters.ListarAsientosDisponibles
{
    // ViewModel de la página de asientos de un evento. El presenter lo arma a partir del
    // OutputDataResponse y la vista solo lo recorre, así los textos, los colores, el formato
    // de la fecha y la agrupación por fila se deciden en el anillo 3 y no en Razor.
    // No incluye ningún Guid porque el visitante identifica un asiento por fila y número.
    // El Id del asiento va a volver como dato oculto del formulario cuando exista el caso de
    // uso de reservar.
    public class ListarAsientosViewModel
    {
        private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-AR");

        // Las fechas se guardan en UTC y se muestran en hora argentina. Se usa el
        // identificador IANA de la zona, que .NET reconoce en Windows y en Linux.
        private static readonly TimeZoneInfo ZonaArgentina =
            TimeZoneInfo.FindSystemTimeZoneById("America/Argentina/Buenos_Aires");

        public ListarAsientosViewModel(OutputDataResponse response)
        {
            EventoEncontrado = response.EventoEncontrado;

            if (!response.EventoEncontrado)
            {
                MensajeEventoNoEncontrado =
                    "No encontramos el evento que buscás. Revisá que el enlace sea correcto.";
                return;
            }

            NombreEvento = response.NombreEvento;
            FechaTexto = FormatearFecha(response.Fecha);

            if (!response.OcupacionDisponible)
            {
                AvisoOcupacion =
                    "En este momento no podemos confirmar qué asientos están libres. " +
                    "Volvé a intentar en unos minutos.";
            }

            // El Interactor entrega los asientos ordenados por fila y número, y GroupBy
            // respeta ese orden, así que el presenter no vuelve a ordenar.
            Filas = response.Asientos
                .GroupBy(a => a.Fila)
                .Select(grupo => new FilaViewModel
                {
                    Nombre = $"Fila {grupo.Key}",
                    Asientos = grupo.Select(a => new AsientoViewModel(a)).ToList()
                })
                .ToList();
        }

        public bool EventoEncontrado { get; internal set; }

        public string? MensajeEventoNoEncontrado { get; internal set; }

        public string NombreEvento { get; internal set; } = string.Empty;

        public string FechaTexto { get; internal set; } = string.Empty;

        public string? AvisoOcupacion { get; internal set; }

        public List<FilaViewModel> Filas { get; internal set; } = new();

        // Por ejemplo, "sábado, 12 de diciembre de 2026, 20:00 h".
        private static string FormatearFecha(DateTime fechaUtc)
        {
            // SQLite no guarda si una fecha es UTC o local, y al leerla el tipo llega sin
            // especificar. Se marca como UTC antes de convertirla a la hora argentina.
            DateTime utc = DateTime.SpecifyKind(fechaUtc, DateTimeKind.Utc);
            DateTime local = TimeZoneInfo.ConvertTimeFromUtc(utc, ZonaArgentina);
            return local.ToString("dddd, d 'de' MMMM 'de' yyyy, HH:mm 'h'", Cultura);
        }
    }

    public class FilaViewModel
    {
        public string Nombre { get; internal set; } = string.Empty;

        public List<AsientoViewModel> Asientos { get; internal set; } = new();
    }

    public class AsientoViewModel
    {
        public AsientoViewModel(AsientoDelEvento asiento)
        {
            Etiqueta = $"{asiento.Fila}{asiento.Numero}";

            // El texto para el visitante y el color del asiento son decisiones de
            // presentación, por eso se toman acá y no en el Interactor ni en la vista. Las
            // clases son de Bootstrap, la librería de estilos que trae la plantilla MVC.
            (EstadoTexto, ClaseCss) = asiento.Estado switch
            {
                EstadoAsiento.Disponible => ("Disponible", "bg-success"),
                EstadoAsiento.Pendiente => ("Pendiente de pago", "bg-warning text-dark"),
                EstadoAsiento.Reservado => ("Reservado", "bg-danger"),
                EstadoAsiento.Desconocido => ("Sin información", "bg-secondary"),
                _ => throw new ArgumentOutOfRangeException(nameof(asiento), asiento.Estado, "Estado de asiento no contemplado.")
            };
        }

        // Fila y número juntos, como figura el asiento en una entrada, por ejemplo A2.
        public string Etiqueta { get; internal set; }

        public string EstadoTexto { get; internal set; }

        public string ClaseCss { get; internal set; }
    }
}