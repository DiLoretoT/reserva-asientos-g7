namespace Booking.Business;

public class BookingOptions
{
    public const string SectionName = "Booking";

    public int RetentionMinutes { get; set; }

    public int ExpirationIntervalSeconds { get; set; }
    public decimal MontoReserva { get; set; }
}
