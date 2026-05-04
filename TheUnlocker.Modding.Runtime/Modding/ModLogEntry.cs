namespace TheUnlocker.Modding;

public sealed class ModLogEntry
{
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.Now;

    public string ModId { get; init; } = "system";

    public string Severity { get; init; } = "Info";

    public string EventType { get; init; } = "Runtime";

    public string Message { get; init; } = "";
}
