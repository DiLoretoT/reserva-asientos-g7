using System.Text.Json;
using System.Text.Json.Serialization;

namespace Booking.Api;

public static class BookingJson
{
    public static void Configurar(JsonSerializerOptions options)
    {
        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower,
            allowIntegerValues: false));
    }
}
