using System.Text.Json;
using System.Text.Json.Serialization;

namespace Booking.Api;

// Define el formato JSON del contrato en un solo lugar: camelCase y enums como texto (por ejemplo "pago_fallido")
// y no como número, así el JSON se entiende sin mirar el código. Lo usan los controllers y los endpoints mínimos,
// y los dos serializan igual.
public static class BookingJson
{
    public static void Configurar(JsonSerializerOptions options)
    {
        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower,
            // allowIntegerValues false: rechaza números como estado, así no se acepta un formato que el
            // contrato no define.
            allowIntegerValues: false));
    }
}
