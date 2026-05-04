namespace TheUnlocker.ContentManagement;

public sealed class ModuleManifest
{
    public string Id { get; init; } = "";

    public string? Name { get; init; }

    public string? Version { get; init; }

    public string? EntryDll { get; init; }
}
