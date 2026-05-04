using TheUnlocker.Modding;

namespace TheUnlocker.Desktop;

public sealed class LogSearchQuery
{
    public string? Text { get; init; }
    public string? ModId { get; init; }
    public string? Severity { get; init; }
    public string? EventType { get; init; }
    public DateTimeOffset? Since { get; init; }
}

public sealed class AdvancedLogSearch
{
    public IReadOnlyCollection<ModLogEntry> Search(IEnumerable<ModLogEntry> logs, LogSearchQuery query)
    {
        var results = logs;
        if (!string.IsNullOrWhiteSpace(query.Text))
        {
            results = results.Where(x => x.Message.Contains(query.Text, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(query.ModId))
        {
            results = results.Where(x => x.ModId.Equals(query.ModId, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(query.Severity))
        {
            results = results.Where(x => x.Severity.Equals(query.Severity, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(query.EventType))
        {
            results = results.Where(x => x.EventType.Equals(query.EventType, StringComparison.OrdinalIgnoreCase));
        }

        if (query.Since is not null)
        {
            results = results.Where(x => x.Timestamp >= query.Since);
        }

        return results.OrderByDescending(x => x.Timestamp).ToList();
    }
}
