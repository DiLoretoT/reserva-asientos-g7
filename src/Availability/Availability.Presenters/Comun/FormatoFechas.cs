using System.Globalization;

namespace Availability.Presenters.Comun
{
    // Formato de fechas compartido por los presenters. Antes estaba dentro de
    // ListarAsientosViewModel; al sumar el listado de eventos pasó a un solo lugar, así las
    // dos páginas muestran la misma fecha de la misma manera y la conversión de zona horaria
    // no se repite.
    internal static class FormatoFechas
    {
        private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-AR");

        // Las fechas se guardan en UTC y se muestran en hora argentina. Se usa el
        // identificador IANA de la zona, que .NET reconoce en Windows y en Linux.
        private static readonly TimeZoneInfo ZonaArgentina =
            TimeZoneInfo.FindSystemTimeZoneById("America/Argentina/Buenos_Aires");

        // SQLite no guarda si una fecha es UTC o local, y al leerla el tipo llega sin
        // especificar. Se marca como UTC antes de convertirla.
        public static DateTime AHoraArgentina(DateTime fechaUtc) =>
            TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(fechaUtc, DateTimeKind.Utc), ZonaArgentina);

        // Por ejemplo, "sábado, 12 de diciembre de 2026, 20:00 h".
        public static string Completa(DateTime fechaUtc) =>
            AHoraArgentina(fechaUtc).ToString("dddd, d 'de' MMMM 'de' yyyy, HH:mm 'h'", Cultura);

        // Por ejemplo, "sáb 20:00 h", para acompañar el día y el mes en la tarjeta del evento.
        public static string DiaYHora(DateTime fechaUtc) =>
            AHoraArgentina(fechaUtc).ToString("ddd HH:mm 'h'", Cultura).Replace(".", "");

        // Por ejemplo, "dic". Se quita el punto que agrega la cultura es-AR a la abreviatura.
        public static string MesAbreviado(DateTime fechaUtc) =>
            AHoraArgentina(fechaUtc).ToString("MMM", Cultura).Replace(".", "");

        public static string Dia(DateTime fechaUtc) =>
            AHoraArgentina(fechaUtc).Day.ToString(Cultura);
    }
}
