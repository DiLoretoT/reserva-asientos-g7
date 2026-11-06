namespace Booking.Api.Broker;

public class ServiceBusOptions
{
    public const string SectionName = "ServiceBus";
    public bool Enabled { get; set; }
    public string? ConnectionString { get; set; }
    public string TopicName { get; set; } = "grupo7-booking-topic";
    public string SubscriptionName { get; set; } = "";
}
