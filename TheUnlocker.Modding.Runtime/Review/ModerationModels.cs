namespace TheUnlocker.Review;

public enum ModerationStatus
{
    Submitted,
    Scanned,
    Approved,
    Rejected,
    Quarantined
}

public sealed class ModerationRecord
{
    public string ModId { get; init; } = "";
    public string Version { get; init; } = "";
    public ModerationStatus Status { get; init; } = ModerationStatus.Submitted;
    public string Reviewer { get; init; } = "";
    public string Notes { get; init; } = "";
    public DateTimeOffset UpdatedAt { get; init; } = DateTimeOffset.UtcNow;
    public string[] Flags { get; init; } = [];
}
