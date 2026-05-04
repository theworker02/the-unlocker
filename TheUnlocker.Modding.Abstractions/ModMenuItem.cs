namespace TheUnlocker.Modding;

public sealed class ModMenuItem
{
    public required string ModId { get; init; }

    public required string Title { get; init; }

    public required Action Execute { get; init; }
}
