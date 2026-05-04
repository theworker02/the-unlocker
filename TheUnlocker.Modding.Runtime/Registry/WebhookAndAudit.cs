namespace TheUnlocker.Registry;

public sealed class RegistryWebhook
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string TargetUrl { get; init; } = "";
    public string[] Events { get; init; } = [];
    public string Secret { get; init; } = "";
}

public sealed class RegistryWebhookEvent
{
    public string EventType { get; init; } = "";
    public object Payload { get; init; } = new();
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}

public sealed class AuditLogEntry
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string ActorId { get; init; } = "";
    public string Role { get; init; } = "";
    public string Action { get; init; } = "";
    public string Target { get; init; } = "";
    public string IpAddress { get; init; } = "";
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}
