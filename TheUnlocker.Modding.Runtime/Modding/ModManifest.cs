namespace TheUnlocker.Modding;

public sealed class ModManifest
{
    public string Id { get; init; } = "";

    public string Name { get; init; } = "";

    public string Version { get; init; } = "1.0.0";

    public string Author { get; init; } = "";

    public string Description { get; init; } = "";

    public string EntryDll { get; init; } = "";

    public string? MinimumAppVersion { get; init; }

    public string? MinimumFrameworkVersion { get; init; }

    public string SdkVersion { get; init; } = "1.0.0";

    public string[] DependsOn { get; init; } = [];

    public ModDependency[] Dependencies { get; init; } = [];

    public ModDependency[] PeerDependencies { get; init; } = [];

    public string[] Permissions { get; init; } = [];

    public string[] Targets { get; init; } = [];

    public Dictionary<string, ModSettingDefinition> Settings { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    public string? PublisherId { get; init; }

    public ModTrustLevel TrustLevel { get; init; } = ModTrustLevel.Unknown;

    public ModIsolationMode IsolationMode { get; init; } = ModIsolationMode.InProcess;

    public string[] EventSchemas { get; init; } = [];

    public Dictionary<string, string[]> CommandScopes { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    public ModSignature? Signature { get; init; }
}
