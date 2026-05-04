namespace TheUnlocker.Desktop;

public sealed class WindowsInstallerPlan
{
    public string InstallerKind { get; init; } = "Velopack";
    public string RuntimeIdentifier { get; init; } = "win-x64";
    public string PackageId { get; init; } = "TheUnlocker";
    public string OutputDirectory { get; init; } = "artifacts/installer";

    public string BuildCommand => $"dotnet publish TheUnlocker/TheUnlocker.csproj -c Release -r {RuntimeIdentifier} --self-contained false";
    public string PackCommand => $"vpk pack -u {PackageId} -v 1.1.2 -p TheUnlocker/bin/Release/net8.0-windows/{RuntimeIdentifier}/publish -o {OutputDirectory}";
}

public sealed class DesktopUpdatePolicy
{
    public string Channel { get; init; } = "stable";
    public Uri FeedUrl { get; init; } = new("https://updates.example.invalid/theunlocker/stable");
    public bool AllowPrerelease { get; init; }
    public TimeSpan CheckInterval { get; init; } = TimeSpan.FromHours(12);
}

public sealed class DesktopUpdateCheckResult
{
    public bool UpdateAvailable { get; init; }
    public string Version { get; init; } = "";
    public Uri? DownloadUrl { get; init; }
}

public interface IDesktopUpdateService
{
    Task<DesktopUpdateCheckResult> CheckAsync(DesktopUpdatePolicy policy, CancellationToken cancellationToken = default);
}
