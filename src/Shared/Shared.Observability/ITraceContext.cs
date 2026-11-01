namespace Shared.Observability;

public interface ITraceContext
{
    string? TraceId { get; set; }
}

public sealed class TraceContext : ITraceContext
{
    private static readonly AsyncLocal<string?> Actual = new();

    public string? TraceId
    {
        get => Actual.Value;
        set => Actual.Value = value;
    }
}