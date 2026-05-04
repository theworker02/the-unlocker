namespace TheUnlocker.CrashReporting;

public sealed class CrashReport
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public DateTimeOffset SubmittedAt { get; init; } = DateTimeOffset.UtcNow;
    public string UserId { get; init; } = "";
    public string GameId { get; init; } = "";
    public string AppVersion { get; init; } = "";
    public string Summary { get; init; } = "";
    public string DiagnosticsBundleName { get; init; } = "";
    public string[] SuspectedModIds { get; init; } = [];
}
