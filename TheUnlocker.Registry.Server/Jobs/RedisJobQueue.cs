using System.Text.Json;
using StackExchange.Redis;

namespace TheUnlocker.Registry.Server.Jobs;

public sealed class RegistryJob
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string Type { get; init; } = "";
    public object Payload { get; init; } = new();
    public DateTimeOffset QueuedAt { get; init; } = DateTimeOffset.UtcNow;
}

public interface IRegistryJobQueue
{
    Task EnqueueAsync(RegistryJob job, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<RegistryJob>> PeekAsync(string queueName, int count = 25, CancellationToken cancellationToken = default);
    Task<RegistryJob?> DequeueAsync(string queueName, CancellationToken cancellationToken = default);
    Task DeadLetterAsync(RegistryJob job, string reason, CancellationToken cancellationToken = default);
}

public sealed class RedisJobQueue : IRegistryJobQueue, IAsyncDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly ConnectionMultiplexer _redis;
    private readonly IDatabase _database;

    public RedisJobQueue(string connectionString)
    {
        _redis = ConnectionMultiplexer.Connect(connectionString);
        _database = _redis.GetDatabase();
    }

    public Task EnqueueAsync(RegistryJob job, CancellationToken cancellationToken = default)
    {
        var queue = QueueName(job.Type);
        var json = JsonSerializer.Serialize(job, JsonOptions);
        return _database.ListRightPushAsync(queue, json);
    }

    public async Task<IReadOnlyCollection<RegistryJob>> PeekAsync(string queueName, int count = 25, CancellationToken cancellationToken = default)
    {
        var values = await _database.ListRangeAsync(QueueName(queueName), 0, count - 1);
        return values
            .Where(value => value.HasValue)
            .Select(value => JsonSerializer.Deserialize<RegistryJob>(value!, JsonOptions))
            .Where(job => job is not null)
            .Cast<RegistryJob>()
            .ToList();
    }

    public async Task<RegistryJob?> DequeueAsync(string queueName, CancellationToken cancellationToken = default)
    {
        var value = await _database.ListLeftPopAsync(QueueName(queueName));
        return value.HasValue
            ? JsonSerializer.Deserialize<RegistryJob>(value!, JsonOptions)
            : null;
    }

    public Task DeadLetterAsync(RegistryJob job, string reason, CancellationToken cancellationToken = default)
    {
        var failed = new { job, reason, failedAt = DateTimeOffset.UtcNow };
        return _database.ListRightPushAsync($"{QueueName(job.Type)}:dead-letter", JsonSerializer.Serialize(failed, JsonOptions));
    }

    public async ValueTask DisposeAsync()
    {
        await _redis.DisposeAsync();
    }

    private static string QueueName(string type) => $"theunlocker:jobs:{type.ToLowerInvariant()}";
}

public sealed class InMemoryJobQueue : IRegistryJobQueue
{
    private readonly Dictionary<string, List<RegistryJob>> _jobs = new(StringComparer.OrdinalIgnoreCase);

    public Task EnqueueAsync(RegistryJob job, CancellationToken cancellationToken = default)
    {
        var queue = job.Type;
        if (!_jobs.TryGetValue(queue, out var jobs))
        {
            jobs = [];
            _jobs[queue] = jobs;
        }

        jobs.Add(job);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<RegistryJob>> PeekAsync(string queueName, int count = 25, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyCollection<RegistryJob>>(
            _jobs.TryGetValue(queueName, out var jobs)
                ? jobs.Take(count).ToList()
                : []);
    }

    public Task<RegistryJob?> DequeueAsync(string queueName, CancellationToken cancellationToken = default)
    {
        if (!_jobs.TryGetValue(queueName, out var jobs) || jobs.Count == 0)
        {
            return Task.FromResult<RegistryJob?>(null);
        }

        var job = jobs[0];
        jobs.RemoveAt(0);
        return Task.FromResult<RegistryJob?>(job);
    }

    public Task DeadLetterAsync(RegistryJob job, string reason, CancellationToken cancellationToken = default)
    {
        var queue = $"{job.Type}:dead-letter";
        if (!_jobs.TryGetValue(queue, out var jobs))
        {
            jobs = [];
            _jobs[queue] = jobs;
        }

        jobs.Add(job);
        return Task.CompletedTask;
    }
}
