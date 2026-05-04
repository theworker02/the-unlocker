namespace TheUnlocker.Modding;

public sealed class ModUpdateInfo
{
    public required string ModId { get; init; }

    public required string InstalledVersion { get; init; }

    public required string AvailableVersion { get; init; }

    public required string DownloadUrl { get; init; }

    public string Changelog { get; init; } = "";

    public string NewPermissions { get; init; } = "";
}
