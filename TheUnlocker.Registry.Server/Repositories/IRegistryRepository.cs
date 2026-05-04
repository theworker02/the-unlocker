using TheUnlocker.CrashReporting;
using TheUnlocker.Registry;
using TheUnlocker.Review;

namespace TheUnlocker.Registry.Server.Repositories;

public interface IRegistryRepository
{
    string ProviderName { get; }
    Task<IReadOnlyCollection<object>> SearchModsAsync(string? text, string? gameId, string? tag, CancellationToken cancellationToken = default);
    Task SaveAuditAsync(AuditLogEntry entry, CancellationToken cancellationToken = default);
    Task SaveWebhookEventAsync(RegistryWebhookEvent webhookEvent, CancellationToken cancellationToken = default);
    Task SaveCrashReportAsync(CrashReport report, CancellationToken cancellationToken = default);
    Task SaveModerationAsync(ModerationRecord record, CancellationToken cancellationToken = default);
}

public sealed class JsonRegistryRepository : IRegistryRepository
{
    public string ProviderName => "Json";
    public Task<IReadOnlyCollection<object>> SearchModsAsync(string? text, string? gameId, string? tag, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<object>>([]);
    public Task SaveAuditAsync(AuditLogEntry entry, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task SaveWebhookEventAsync(RegistryWebhookEvent webhookEvent, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task SaveCrashReportAsync(CrashReport report, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task SaveModerationAsync(ModerationRecord record, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
