namespace TheUnlocker.Modpacks;

public sealed class ModpackProduct
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string Version { get; init; } = "1.0.0";
    public string[] Maintainers { get; init; } = [];
    public string Changelog { get; init; } = "";
    public string[] Screenshots { get; init; } = [];
    public string LockfileUrl { get; init; } = "";
    public string CompatibilityMatrixUrl { get; init; } = "";
    public string InstallUri => $"theunlocker://install-pack/{Id}";
}

public sealed class ModpackRollbackPoint
{
    public string ModpackId { get; init; } = "";
    public string Version { get; init; } = "";
    public string LockfileSnapshotPath { get; init; } = "";
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}

public sealed class ModpackCatalogService
{
    public ModpackProduct Promote(string id, string name, string version, IEnumerable<string> maintainers, string lockfileUrl)
    {
        return new ModpackProduct
        {
            Id = id,
            Name = name,
            Version = version,
            Maintainers = maintainers.ToArray(),
            LockfileUrl = lockfileUrl,
            Changelog = "Promoted from workspace lockfile."
        };
    }
}
