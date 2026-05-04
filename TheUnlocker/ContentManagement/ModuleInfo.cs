namespace TheUnlocker.ContentManagement;

public sealed class ModuleInfo
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required string Path { get; init; }

    public string? Version { get; init; }

    public ModuleStatus Status { get; set; }

    public string? ErrorMessage { get; set; }
}
