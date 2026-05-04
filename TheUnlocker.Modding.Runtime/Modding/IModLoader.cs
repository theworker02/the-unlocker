namespace TheUnlocker.Modding;

public interface IModLoader
{
    IReadOnlyCollection<LoadedModInfo> LoadedMods { get; }

    IReadOnlyCollection<string> Logs { get; }

    IReadOnlyCollection<ModLogEntry> LogEntries { get; }

    IReadOnlyCollection<ModSettingInfo> ModSettings { get; }

    IReadOnlyCollection<ModLoadOrderInfo> LoadOrder { get; }

    IReadOnlyCollection<ModConflictInfo> Conflicts { get; }

    IReadOnlyCollection<ModHealthInfo> Health { get; }

    IReadOnlyCollection<ModUpdateInfo> Updates { get; }

    IReadOnlyCollection<ModRepositoryEntry> Marketplace { get; }

    IReadOnlyCollection<ModRegistryEntry> RegistryEntries { get; }

    IReadOnlyCollection<string> Profiles { get; }

    string ActiveProfile { get; }

    void LoadMods();

    void SetModEnabled(string modId, bool enabled);

    void SetModSetting(string modId, string key, string value);

    void SetActiveProfile(string profileName);

    void SaveProfile(string profileName);

    void RefreshUpdates();

    string InstallMod(string sourcePath);

    string InstallMarketplaceMod(string modId);
    string InstallModpack(string lockfileUrlOrPath);
    string RollbackMod(string modId, string packagePath);
    string ExportDependencyGraph(string outputPath);

    string ExportDiagnostics(string outputDirectory);

    string GenerateCompatibilityReport(string outputDirectory);

    string GenerateModDocumentation(string modId, string outputDirectory);

    void UnloadMods();
}
