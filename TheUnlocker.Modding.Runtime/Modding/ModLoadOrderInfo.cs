namespace TheUnlocker.Modding;

public sealed class ModLoadOrderInfo
{
    public required int Order { get; init; }

    public required string ModId { get; init; }

    public required string Reason { get; init; }
}
