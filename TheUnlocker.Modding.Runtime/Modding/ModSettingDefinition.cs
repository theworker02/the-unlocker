namespace TheUnlocker.Modding;

public sealed class ModSettingDefinition
{
    public string Label { get; init; } = "";

    public string Type { get; init; } = "text";

    public string DefaultValue { get; init; } = "";

    public string[] Options { get; init; } = [];
}
