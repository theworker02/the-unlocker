namespace TheUnlocker.Desktop;

public sealed class QuarantinedPackageView
{
    public string PackageId { get; init; } = "";
    public string Version { get; init; } = "";
    public string Reason { get; init; } = "";
    public string OriginalPath { get; init; } = "";
    public string QuarantinePath { get; init; } = "";
    public DateTimeOffset QuarantinedAt { get; init; } = DateTimeOffset.UtcNow;
}

public sealed class QuarantineReviewService
{
    public QuarantinedPackageView[] List(string quarantineDirectory)
    {
        if (!Directory.Exists(quarantineDirectory))
        {
            return [];
        }

        return Directory.EnumerateFiles(quarantineDirectory, "*.zip", SearchOption.TopDirectoryOnly)
            .Select(path => new QuarantinedPackageView
            {
                PackageId = Path.GetFileNameWithoutExtension(path),
                Version = "unknown",
                Reason = "Awaiting review",
                QuarantinePath = path
            })
            .ToArray();
    }
}
