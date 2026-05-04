namespace TheUnlocker.Remote;

public sealed class RemoteDesktopClient
{
    public string ClientId { get; init; } = Guid.NewGuid().ToString("N");
    public string DisplayName { get; init; } = "";
    public string Address { get; init; } = "";
    public string Platform { get; init; } = "";
    public bool IsOnline { get; init; }
}

public sealed class RemoteInstallCommand
{
    public string CommandId { get; init; } = Guid.NewGuid().ToString("N");
    public string ClientId { get; init; } = "";
    public string PackageId { get; init; } = "";
    public string Version { get; init; } = "";
    public string SourceRegistry { get; init; } = "";
}

public sealed class RemoteOrchestrationService
{
    public RemoteInstallCommand CreateInstallCommand(RemoteDesktopClient client, string packageId, string version, string registry)
    {
        if (!client.IsOnline)
        {
            throw new InvalidOperationException($"Remote client {client.DisplayName} is offline.");
        }

        return new RemoteInstallCommand
        {
            ClientId = client.ClientId,
            PackageId = packageId,
            Version = version,
            SourceRegistry = registry
        };
    }
}
