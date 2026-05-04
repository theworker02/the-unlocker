namespace TheUnlocker.Modding;

public sealed class ModRegistryEntry
{
    public string Id { get; init; } = "";

    public string Version { get; init; } = "";

    public string PackagePath { get; init; } = "";

    public string Source { get; init; } = "";

    public DateTimeOffset InstalledAt { get; init; } = DateTimeOffset.Now;

    public string Sha256 { get; init; } = "";
}
