namespace Booking.Data.Payment;

public class PaymentOptions
{
    public const string SectionName = "Payment";
    public string Address { get; set; } = "";
    public double DeadlineSeconds { get; set; }
}
