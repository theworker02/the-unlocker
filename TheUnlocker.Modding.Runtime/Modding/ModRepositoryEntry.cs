namespace TheUnlocker.Modding;

public sealed class ModRepositoryEntry
{
    public string Id { get; init; } = "";

    public string Version { get; init; } = "";

    public string DownloadUrl { get; init; } = "";

    public string Sha256 { get; init; } = "";

    public string Name { get; init; } = "";

    public string Description { get; init; } = "";

    public string[] Permissions { get; init; } = [];

    public string Changelog { get; init; } = "";
}
