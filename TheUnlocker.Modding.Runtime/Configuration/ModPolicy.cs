namespace TheUnlocker.Configuration;

public sealed class ModPolicy
{
    public bool AllowUnsignedMods { get; set; } = true;

    public HashSet<string> AllowedPublishers { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    public HashSet<string> BlockedMods { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    public HashSet<string> PrivateRegistryUrls { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, bool> PermissionDefaults { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}
