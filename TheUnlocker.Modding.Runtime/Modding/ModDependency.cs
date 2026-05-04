namespace TheUnlocker.Modding;

public sealed class ModDependency
{
    public string Id { get; init; } = "";

    public string VersionRange { get; init; } = "";

    public bool Optional { get; init; }
}
