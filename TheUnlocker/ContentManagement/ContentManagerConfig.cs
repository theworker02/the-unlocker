namespace TheUnlocker.ContentManagement;

// Kept for compatibility with earlier examples. Runtime state now lives in Configuration.LocalAppConfig.
public sealed class ContentManagerConfig
{
    public HashSet<string> EnabledModules { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}
