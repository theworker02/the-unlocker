using TheUnlocker.Modding;

namespace TheUnlocker.Desktop;

public sealed class FirstRunSetupState
{
    public bool HasCompletedSetup { get; init; }
    public string[] DetectedGamePaths { get; init; } = [];
    public string ModsDirectory { get; init; } = "";
    public string RegistryUrl { get; init; } = "";
    public bool SafeMode { get; init; }
    public bool AllowUnsignedMods { get; init; } = true;
}

public sealed class InstallQueueItem
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string Source { get; init; } = "";
    public string Status { get; set; } = "Queued";
    public int Attempts { get; set; }
    public string LastError { get; set; } = "";
}

public sealed class PermissionTimelineEntry
{
    public string ModId { get; init; } = "";
    public string Version { get; init; } = "";
    public string[] AddedPermissions { get; init; } = [];
    public string[] RemovedPermissions { get; init; } = [];
    public DateTimeOffset RecordedAt { get; init; } = DateTimeOffset.UtcNow;
}

public sealed class PerGameDashboard
{
    public string GameId { get; init; } = "";
    public string Profile { get; init; } = "";
    public int InstalledMods { get; init; }
    public int Conflicts { get; init; }
    public int Warnings { get; init; }
}

public sealed class NotificationItem
{
    public string Severity { get; init; } = "Info";
    public string Source { get; init; } = "";
    public string Message { get; init; } = "";
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.Now;
}

public sealed class ExtensionContributionInfo
{
    public string ModId { get; init; } = "";
    public string Menus { get; init; } = "";
    public string Commands { get; init; } = "";
    public string Themes { get; init; } = "";
    public string Panels { get; init; } = "";
    public string Importers { get; init; } = "";
}

public sealed class ModDiffResult
{
    public string ModId { get; init; } = "";
    public string FromVersion { get; init; } = "";
    public string ToVersion { get; init; } = "";
    public string[] PermissionChanges { get; init; } = [];
    public string[] DependencyChanges { get; init; } = [];
    public string[] TargetChanges { get; init; } = [];
    public string Changelog { get; init; } = "";
}

public sealed class ModDiffService
{
    public ModDiffResult Compare(ModManifest from, ModManifest to, string changelog)
    {
        return new ModDiffResult
        {
            ModId = to.Id,
            FromVersion = from.Version,
            ToVersion = to.Version,
            PermissionChanges = Diff(from.Permissions, to.Permissions),
            DependencyChanges = Diff(
                from.Dependencies.Select(x => $"{x.Id} {x.VersionRange}").Concat(from.DependsOn),
                to.Dependencies.Select(x => $"{x.Id} {x.VersionRange}").Concat(to.DependsOn)),
            TargetChanges = Diff(from.Targets, to.Targets),
            Changelog = changelog
        };
    }

    private static string[] Diff(IEnumerable<string> before, IEnumerable<string> after)
    {
        var oldSet = before.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var newSet = after.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return newSet.Except(oldSet, StringComparer.OrdinalIgnoreCase).Select(x => $"+ {x}")
            .Concat(oldSet.Except(newSet, StringComparer.OrdinalIgnoreCase).Select(x => $"- {x}"))
            .ToArray();
    }
}
