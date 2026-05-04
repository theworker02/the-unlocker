namespace TheUnlocker.Modding;

public sealed class ModSettingInfo
{
    public required string ModId { get; init; }

    public required string Key { get; init; }

    public required string Label { get; init; }

    public required string Type { get; init; }

    public required string Value { get; init; }

    public string[] Options { get; init; } = [];
}
