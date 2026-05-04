namespace TheUnlocker.Observability;

public sealed class ModRuntimeMetric
{
    public string ModId { get; init; } = "";
    public TimeSpan LoadDuration { get; init; }
    public long MemoryDeltaBytes { get; init; }
    public int EventHandlersRegistered { get; init; }
    public int CommandsAdded { get; init; }
    public int ExceptionsThrown { get; init; }
    public double? FpsImpact { get; init; }
    public DateTimeOffset LastSuccessfulLoad { get; init; } = DateTimeOffset.MinValue;
}

public sealed class RuntimeObservabilityService
{
    public string Summarize(ModRuntimeMetric metric)
    {
        var fps = metric.FpsImpact is null ? "unknown FPS impact" : $"{metric.FpsImpact:0.0} FPS impact";
        return $"{metric.ModId}: load {metric.LoadDuration.TotalMilliseconds:0}ms, memory {metric.MemoryDeltaBytes} bytes, {metric.EventHandlersRegistered} handlers, {metric.CommandsAdded} commands, {metric.ExceptionsThrown} exceptions, {fps}.";
    }
}
