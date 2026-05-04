namespace TheUnlocker.Modding;

public sealed class CapabilityToken
{
    public CapabilityToken(string modId, IReadOnlySet<string> permissions)
    {
        ModId = modId;
        Permissions = permissions;
    }

    public string ModId { get; }

    public IReadOnlySet<string> Permissions { get; }
}
