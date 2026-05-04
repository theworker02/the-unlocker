namespace TheUnlocker.Modding;

public sealed class LoadedModInfo
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required string Version { get; init; }

    public string Author { get; init; } = "";

    public string Description { get; init; } = "";

    public required string AssemblyPath { get; init; }

    public required ModLoadStatus Status { get; init; }

    public ModSignatureStatus SignatureStatus { get; init; } = ModSignatureStatus.Unsigned;

    public bool IsEnabled { get; init; }

    public string[] Dependencies { get; init; } = [];

    public string[] Permissions { get; init; } = [];

    public string[] Targets { get; init; } = [];

    public Dictionary<string, ModSettingDefinition> Settings { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    public string? Message { get; init; }

    public IMod? Instance { get; init; }

    public ModAssemblyLoadContext? LoadContext { get; init; }
}
