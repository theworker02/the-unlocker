namespace TheUnlocker.Desktop;

public enum ReleaseChannel
{
    Stable,
    Beta,
    Nightly
}

public sealed class DesktopReleaseInfo
{
    public string Version { get; init; } = "";
    public ReleaseChannel Channel { get; init; } = ReleaseChannel.Stable;
    public string DownloadUrl { get; init; } = "";
    public string Sha256 { get; init; } = "";
    public string SignatureUrl { get; init; } = "";
    public string Changelog { get; init; } = "";
    public bool HealthCheckPassed { get; init; }
}

public sealed class DesktopSelfUpdater
{
    public DesktopReleaseInfo? SelectUpdate(IEnumerable<DesktopReleaseInfo> releases, ReleaseChannel channel, Version currentVersion)
    {
        return releases
            .Where(release => release.Channel == channel)
            .Where(release => release.HealthCheckPassed)
            .Where(release => Version.TryParse(release.Version, out var version) && version > currentVersion)
            .OrderByDescending(release => Version.Parse(release.Version))
            .FirstOrDefault();
    }

    public string CreateRollbackPlan(DesktopReleaseInfo release)
    {
        return $"Install signed {release.Channel} release {release.Version}; run health check; rollback automatically if launch fails.";
    }

    public bool IsAllowedByPolicy(DesktopReleaseInfo release, bool requireSignedUpdates, bool allowPrerelease)
    {
        if (requireSignedUpdates && string.IsNullOrWhiteSpace(release.SignatureUrl))
        {
            return false;
        }

        if (!allowPrerelease && release.Channel is ReleaseChannel.Beta or ReleaseChannel.Nightly)
        {
            return false;
        }

        return release.HealthCheckPassed;
    }
}
