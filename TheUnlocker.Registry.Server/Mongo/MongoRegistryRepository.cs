using MongoDB.Driver;
using TheUnlocker.CrashReporting;
using TheUnlocker.Registry;
using TheUnlocker.Review;
using TheUnlocker.Sync;
using TheUnlocker.Registry.Server.Repositories;

namespace TheUnlocker.Registry.Server.Mongo;

public sealed class MongoRegistryRepository : IRegistryRepository
{
    private readonly IMongoDatabase _database;

    public MongoRegistryRepository(IMongoDatabase database)
    {
        _database = database;
    }

    public IMongoCollection<RegistryModDocument> Mods => _database.GetCollection<RegistryModDocument>("mods");
    public IMongoCollection<RegistryUserDocument> Users => _database.GetCollection<RegistryUserDocument>("users");
    public IMongoCollection<ModerationRecord> Moderation => _database.GetCollection<ModerationRecord>("moderation");
    public IMongoCollection<CrashReport> CrashReports => _database.GetCollection<CrashReport>("crash_reports");
    public IMongoCollection<UserSyncState> SyncStates => _database.GetCollection<UserSyncState>("sync_states");
    public IMongoCollection<PackageProvenance> Provenance => _database.GetCollection<PackageProvenance>("provenance");
    public IMongoCollection<VulnerabilityAdvisory> Advisories => _database.GetCollection<VulnerabilityAdvisory>("advisories");
    public IMongoCollection<RegistryWebhook> Webhooks => _database.GetCollection<RegistryWebhook>("webhooks");
    public IMongoCollection<RegistryWebhookEvent> WebhookEvents => _database.GetCollection<RegistryWebhookEvent>("webhook_events");
    public IMongoCollection<AuditLogEntry> AuditLog => _database.GetCollection<AuditLogEntry>("audit_log");
    public IMongoCollection<OrganizationAccount> Organizations => _database.GetCollection<OrganizationAccount>("organizations");
    public IMongoCollection<PublisherTeam> PublisherTeams => _database.GetCollection<PublisherTeam>("publisher_teams");
    public IMongoCollection<PrivateRegistry> PrivateRegistries => _database.GetCollection<PrivateRegistry>("private_registries");
    public string ProviderName => "Mongo";

    public Task<List<RegistryModDocument>> SearchModsAsync(string? text, string? gameId, string? tag, CancellationToken cancellationToken = default)
    {
        var filter = Builders<RegistryModDocument>.Filter.Empty;
        if (!string.IsNullOrWhiteSpace(text))
        {
            filter &= Builders<RegistryModDocument>.Filter.Or(
                Builders<RegistryModDocument>.Filter.Regex(x => x.Name, new MongoDB.Bson.BsonRegularExpression(text, "i")),
                Builders<RegistryModDocument>.Filter.Regex(x => x.Description, new MongoDB.Bson.BsonRegularExpression(text, "i")),
                Builders<RegistryModDocument>.Filter.Regex(x => x.Id, new MongoDB.Bson.BsonRegularExpression(text, "i")));
        }

        if (!string.IsNullOrWhiteSpace(gameId))
        {
            filter &= Builders<RegistryModDocument>.Filter.Eq(x => x.GameId, gameId);
        }

        if (!string.IsNullOrWhiteSpace(tag))
        {
            filter &= Builders<RegistryModDocument>.Filter.AnyEq(x => x.Tags, tag);
        }

        return Mods.Find(filter).SortByDescending(x => x.UpdatedAt).ToListAsync(cancellationToken);
    }

    async Task<IReadOnlyCollection<object>> IRegistryRepository.SearchModsAsync(string? text, string? gameId, string? tag, CancellationToken cancellationToken)
    {
        return (await SearchModsAsync(text, gameId, tag, cancellationToken)).Cast<object>().ToList();
    }

    public Task SaveAuditAsync(AuditLogEntry entry, CancellationToken cancellationToken = default)
        => AuditLog.InsertOneAsync(entry, cancellationToken: cancellationToken);

    public Task SaveWebhookEventAsync(RegistryWebhookEvent webhookEvent, CancellationToken cancellationToken = default)
        => WebhookEvents.InsertOneAsync(webhookEvent, cancellationToken: cancellationToken);

    public Task SaveCrashReportAsync(CrashReport report, CancellationToken cancellationToken = default)
        => CrashReports.InsertOneAsync(report, cancellationToken: cancellationToken);

    public Task SaveModerationAsync(ModerationRecord record, CancellationToken cancellationToken = default)
        => Moderation.InsertOneAsync(record, cancellationToken: cancellationToken);
}
