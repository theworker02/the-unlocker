namespace TheUnlocker.Modding;

public interface IModMigrationContext
{
    string ModId { get; }

    Version FromVersion { get; }

    Version ToVersion { get; }

    IModSettingsService Settings { get; }

    void Log(string message);
}
