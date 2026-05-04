using MongoDB.Driver;

namespace TheUnlocker.Registry.Server.Mongo;

public sealed class MongoRegistryOptions
{
    public string ConnectionString { get; init; } = "mongodb://localhost:27017";
    public string DatabaseName { get; init; } = "theunlocker_registry";
}

public sealed class MongoMigration
{
    public int Version { get; init; }
    public string Name { get; init; } = "";
    public Func<IMongoDatabase, CancellationToken, Task> ApplyAsync { get; init; } = (_, _) => Task.CompletedTask;
    public string RollbackNotes { get; init; } = "Manual rollback required.";
    public string[] CreatedCollections { get; init; } = [];
    public string[] CreatedIndexes { get; init; } = [];
}

public sealed class MongoMigrationRunner
{
    private readonly IMongoDatabase _database;

    public MongoMigrationRunner(IMongoDatabase database)
    {
        _database = database;
    }

    public async Task ApplyAsync(CancellationToken cancellationToken = default)
    {
        await ValidateAsync(cancellationToken);
        var migrations = _database.GetCollection<AppliedMigration>("schema_migrations");
        var applied = (await migrations.Find(_ => true).ToListAsync(cancellationToken))
            .Select(x => x.Version)
            .ToHashSet();

        foreach (var migration in GetMigrations().Where(x => !applied.Contains(x.Version)).OrderBy(x => x.Version))
        {
            await migration.ApplyAsync(_database, cancellationToken);
            await migrations.InsertOneAsync(new AppliedMigration
            {
                Version = migration.Version,
                Name = migration.Name,
                AppliedAt = DateTimeOffset.UtcNow,
                RollbackNotes = migration.RollbackNotes,
                CreatedCollections = migration.CreatedCollections,
                CreatedIndexes = migration.CreatedIndexes
            }, cancellationToken: cancellationToken);
        }
    }

    public async Task ValidateAsync(CancellationToken cancellationToken = default)
    {
        var definitions = GetMigrations().OrderBy(x => x.Version).ToList();
        if (definitions.Select(x => x.Version).Distinct().Count() != definitions.Count)
        {
            throw new InvalidOperationException("Mongo migration versions must be unique.");
        }

        if (definitions.Count > 0 && definitions[0].Version != 1)
        {
            throw new InvalidOperationException("Mongo migrations must start at version 1.");
        }

        for (var index = 1; index < definitions.Count; index++)
        {
            if (definitions[index].Version != definitions[index - 1].Version + 1)
            {
                throw new InvalidOperationException("Mongo migrations must be contiguous.");
            }
        }

        await _database.GetCollection<AppliedMigration>("schema_migrations")
            .Indexes.CreateOneAsync(
                new CreateIndexModel<AppliedMigration>(
                    Builders<AppliedMigration>.IndexKeys.Ascending(x => x.Version),
                    new CreateIndexOptions { Unique = true }),
                cancellationToken: cancellationToken);
    }

    public Task<IReadOnlyCollection<MongoMigration>> GetDefinedMigrationsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(GetMigrations());
    }

    private static IReadOnlyCollection<MongoMigration> GetMigrations()
    {
        return
        [
            new MongoMigration
            {
                Version = 1,
                Name = "create-core-indexes",
                RollbackNotes = "Drop unique indexes on mods.Id and users.Email if a rollback is required.",
                CreatedCollections = ["mods", "users", "schema_migrations"],
                CreatedIndexes = ["mods.Id unique", "users.Email unique"],
                ApplyAsync = async (database, ct) =>
                {
                    await database.GetCollection<RegistryModDocument>("mods").Indexes.CreateOneAsync(
                        new CreateIndexModel<RegistryModDocument>(
                            Builders<RegistryModDocument>.IndexKeys.Ascending(x => x.Id),
                            new CreateIndexOptions { Unique = true }),
                        cancellationToken: ct);
                    await database.GetCollection<RegistryUserDocument>("users").Indexes.CreateOneAsync(
                        new CreateIndexModel<RegistryUserDocument>(
                            Builders<RegistryUserDocument>.IndexKeys.Ascending(x => x.Email),
                            new CreateIndexOptions { Unique = true }),
                        cancellationToken: ct);
                }
            },
            new MongoMigration
            {
                Version = 2,
                Name = "marketplace-search-indexes",
                RollbackNotes = "Drop compound marketplace search index on mods if a rollback is required.",
                CreatedCollections = ["mods"],
                CreatedIndexes = ["mods.GameId+Tags+UpdatedAt"],
                ApplyAsync = async (database, ct) =>
                {
                    await database.GetCollection<RegistryModDocument>("mods").Indexes.CreateOneAsync(
                        new CreateIndexModel<RegistryModDocument>(
                            Builders<RegistryModDocument>.IndexKeys
                                .Ascending(x => x.GameId)
                                .Ascending(x => x.Tags)
                                .Ascending(x => x.UpdatedAt)),
                        cancellationToken: ct);
                }
            }
        ];
    }
}

public sealed class AppliedMigration
{
    public int Version { get; init; }
    public string Name { get; init; } = "";
    public DateTimeOffset AppliedAt { get; init; }
    public string RollbackNotes { get; init; } = "";
    public string[] CreatedCollections { get; init; } = [];
    public string[] CreatedIndexes { get; init; } = [];
}

public sealed class RegistryModDocument
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string Author { get; init; } = "";
    public string Description { get; init; } = "";
    public string Status { get; init; } = "";
    public string GameId { get; init; } = "";
    public string TrustLevel { get; init; } = "Unknown";
    public string[] Tags { get; init; } = [];
    public string[] Permissions { get; init; } = [];
    public List<RegistryVersionDocument> Versions { get; init; } = [];
    public DateTimeOffset UpdatedAt { get; init; } = DateTimeOffset.UtcNow;
}

public sealed class RegistryVersionDocument
{
    public string Version { get; init; } = "";
    public string DownloadUrl { get; init; } = "";
    public string Sha256 { get; init; } = "";
    public string Changelog { get; init; } = "";
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}

public sealed class RegistryUserDocument
{
    public string Id { get; init; } = "";
    public string DisplayName { get; init; } = "";
    public string Email { get; init; } = "";
    public string OAuthProvider { get; init; } = "";
    public string OAuthSubject { get; init; } = "";
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}
