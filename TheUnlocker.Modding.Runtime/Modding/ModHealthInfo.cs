namespace TheUnlocker.Modding;

public sealed class ModHealthInfo
{
    public required string ModId { get; init; }

    public required string Status { get; init; }

    public TimeSpan LoadTime { get; init; }

    public long MemoryDeltaBytes { get; init; }

    public string LastError { get; init; } = "";

    public string ServicesUsed { get; init; } = "";

    public string EnabledDuration { get; init; } = "";

    public int EventHandlersRegistered { get; init; }

    public int CommandsAdded { get; init; }

    public int ExceptionsThrown { get; init; }

    public string LastSuccessfulLoad { get; init; } = "";

    public string AverageLoadTime { get; init; } = "";
}
