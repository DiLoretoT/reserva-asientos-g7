using System.Text;
using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Booking.Business;

namespace Booking.Api.Broker;

public record ValidacionSolicitud(SolicitudReserva? Solicitud, string? MotivoDlq, Guid? TraceId = null);

public static class ValidadorSolicitudServiceBus
{
    private static readonly UTF8Encoding Utf8 = new(false, true);
    private static readonly HashSet<string> Campos =
        ["solicitudId", "eventoId", "asientoId", "usuarioId", "fechaSolicitud"];

    public static ValidacionSolicitud Validar(ServiceBusReceivedMessage mensaje)
    {
        Guid? traceId = null;
        ValidacionSolicitud Invalido(string motivo) => new(null, motivo, traceId);
        try
        {
            using var documento = JsonDocument.Parse(Utf8.GetString(mensaje.Body.ToArray()));
            var raiz = documento.RootElement;
            if (raiz.ValueKind != JsonValueKind.Object)
                return Invalido("campo_invalido");

            if (raiz.TryGetProperty("solicitudId", out var campoId) && campoId.ValueKind == JsonValueKind.String
                && Guid.TryParseExact(campoId.GetString(), "D", out var idTraza) && idTraza != Guid.Empty)
                traceId = idTraza;

            var encontrados = new HashSet<string>(StringComparer.Ordinal);
            foreach (var campo in raiz.EnumerateObject())
            {
                if (!Campos.Contains(campo.Name))
                    continue;

                if (!encontrados.Add(campo.Name) || campo.Value.ValueKind != JsonValueKind.String)
                    return Invalido("campo_invalido");
            }

            if (encontrados.Count != Campos.Count
                || !Identificador(raiz, "solicitudId", out var solicitudId)
                || !Identificador(raiz, "eventoId", out var eventoId)
                || !Identificador(raiz, "asientoId", out var asientoId))
                return Invalido("campo_invalido");

            var usuarioId = raiz.GetProperty("usuarioId").GetString()!;
            var fecha = raiz.GetProperty("fechaSolicitud");
            var textoFecha = fecha.GetString()!;
            if (string.IsNullOrWhiteSpace(usuarioId) || usuarioId.Contains('\0')
                || usuarioId.EnumerateRunes().Count() > 200
                || !(textoFecha.EndsWith('Z') || textoFecha.EndsWith("+00:00", StringComparison.Ordinal))
                || !fecha.TryGetDateTimeOffset(out var fechaSolicitud)
                || fechaSolicitud.Offset != TimeSpan.Zero)
                return Invalido("campo_invalido");

            var solicitud = new SolicitudReserva(solicitudId, eventoId, asientoId, usuarioId);
            var id = solicitudId.ToString("D");
            if (mensaje.MessageId != id || mensaje.CorrelationId != id
                || mensaje.ContentType != "application/json" || mensaje.Subject != "solicitud-reserva")
                return new(solicitud, "campo_invalido", solicitudId);

            return new(solicitud, null, solicitudId);
        }
        catch (Exception ex) when (ex is JsonException or DecoderFallbackException or InvalidOperationException)
        {
            return Invalido("json_invalido");
        }
    }

    private static bool Identificador(JsonElement raiz, string nombre, out Guid id)
        => Guid.TryParseExact(raiz.GetProperty(nombre).GetString(), "D", out id) && id != Guid.Empty;
}
