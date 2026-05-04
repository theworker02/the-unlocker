using TheUnlocker.Configuration;

namespace TheUnlocker.Modding;

public sealed class ModMigrationContext : IModMigrationContext
{
    private readonly Action<string> _log;

    public ModMigrationContext(
        string modId,
        Version fromVersion,
        Version toVersion,
        IModSettingsService settings,
        Action<string> log)
    {
        ModId = modId;
        FromVersion = fromVersion;
        ToVersion = toVersion;
        Settings = settings;
        _log = log;
    }

    public string ModId { get; }

    public Version FromVersion { get; }

    public Version ToVersion { get; }

    public IModSettingsService Settings { get; }

    public void Log(string message)
    {
        _log($"[{ModId}:migration] {message}");
    }
}
