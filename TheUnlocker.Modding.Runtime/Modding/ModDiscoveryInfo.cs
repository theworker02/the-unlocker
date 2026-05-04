namespace TheUnlocker.Modding;

public sealed class ModDiscoveryInfo
{
    public required ModManifest Manifest { get; init; }

    public required string ManifestPath { get; init; }

    public required string DirectoryPath { get; init; }

    public required string EntryDllPath { get; init; }
}
