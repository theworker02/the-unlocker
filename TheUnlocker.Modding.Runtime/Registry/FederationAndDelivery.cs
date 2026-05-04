namespace TheUnlocker.Registry;

public sealed class FederatedRegistryEndpoint
{
    public string Name { get; init; } = "";
    public string BaseUrl { get; init; } = "";
    public string TrustPolicy { get; init; } = "default";
}

public sealed class PackageMirror
{
    public string Region { get; init; } = "";
    public string Url { get; init; } = "";
    public string Sha256 { get; init; } = "";
}

public sealed class CompatibilityPatchListing
{
    public string Id { get; init; } = "";
    public string[] BridgesMods { get; init; } = [];
    public string PatchModId { get; init; } = "";
    public string Description { get; init; } = "";
}

public sealed class CanaryRollout
{
    public string ModId { get; init; } = "";
    public string Version { get; init; } = "";
    public int Percent { get; init; }
    public string Ring { get; init; } = "beta";
}

public sealed class SelfUpdateInfo
{
    public string Version { get; init; } = "";
    public string DownloadUrl { get; init; } = "";
    public string Sha256 { get; init; } = "";
    public string ReleaseNotes { get; init; } = "";
}

public sealed class ObservabilityEvent
{
    public string Source { get; init; } = "";
    public string Name { get; init; } = "";
    public Dictionary<string, string> Tags { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}
