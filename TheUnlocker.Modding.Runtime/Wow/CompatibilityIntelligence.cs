namespace TheUnlocker.Wow;

public sealed class CompatibilitySignal
{
    public string[] ModIds { get; init; } = [];
    public int InstallCount { get; init; }
    public int CrashCount { get; init; }
    public string Warning { get; init; } = "";
}

public sealed class CompatibilityIntelligence
{
    public string GetWarning(CompatibilitySignal signal)
    {
        if (signal.InstallCount < 10)
        {
            return "Not enough data yet.";
        }

        var rate = (double)signal.CrashCount / signal.InstallCount;
        return rate >= 0.2
            ? $"This combination has a high crash rate ({rate:P0})."
            : "No elevated crash pattern detected.";
    }
}

public sealed class AutoCompatibilityPatchSuggestion
{
    public string Id { get; init; } = "";
    public string[] ModIds { get; init; } = [];
    public string LoadOrderRule { get; init; } = "";
    public string Notes { get; init; } = "";
}

public sealed class CloudModpackShare
{
    public string ShareId { get; init; } = Guid.NewGuid().ToString("N");
    public string Name { get; init; } = "";
    public string LockFileUrl { get; init; } = "";
    public string InstallUri => $"theunlocker://install-pack/{ShareId}";
}

public sealed class EnterprisePolicy
{
    public bool LockProfiles { get; init; }
    public bool RequirePrivateRegistry { get; init; }
    public string[] AllowedRegistries { get; init; } = [];
    public string[] AllowlistedMods { get; init; } = [];
    public string[] BlockedMods { get; init; } = [];
    public string AuditLogPath { get; init; } = "";
}
