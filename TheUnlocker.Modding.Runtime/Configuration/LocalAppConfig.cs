namespace TheUnlocker.Configuration;

public sealed class LocalAppConfig
{
    public HashSet<string> EnabledModules { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    public HashSet<string> EnabledMods { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    public HashSet<string> TrustedPublishers { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, Dictionary<string, string>> ModSettings { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, HashSet<string>> Profiles { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    public string ActiveProfile { get; set; } = "Default";

    public string? RepositoryIndexPath { get; set; }

    public Dictionary<string, DateTimeOffset> EnabledSince { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, HashSet<string>> ApprovedPermissions { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, string> LastLoadedVersions { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, int> CrashCounts { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    public HashSet<string> UnsafeMods { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, string> TrustedPublisherKeys { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    public ModPolicy Policy { get; init; } = new();

    public bool SafeMode { get; set; }

    public Dictionary<string, HashSet<string>> LastApprovedPermissions { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}
