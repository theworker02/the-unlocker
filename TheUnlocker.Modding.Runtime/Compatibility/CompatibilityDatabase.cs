using System.Text.Json;

namespace TheUnlocker.Compatibility;

public sealed class CompatibilityRecord
{
    public string ModId { get; init; } = "";
    public string ModVersion { get; init; } = "";
    public string GameId { get; init; } = "";
    public string GameVersion { get; init; } = "";
    public string LoaderVersion { get; init; } = "";
    public string Platform { get; init; } = "";
    public string Status { get; init; } = "unknown";
    public string[] KnownConflicts { get; init; } = [];
    public string Notes { get; init; } = "";
}

public sealed class CompatibilityDatabase
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };
    private readonly string _path;

    public CompatibilityDatabase(string path)
    {
        _path = path;
    }

    public IReadOnlyCollection<CompatibilityRecord> Records => Load();

    public CompatibilityRecord? Find(string modId, string modVersion, string gameId, string gameVersion)
    {
        return Load().FirstOrDefault(x =>
            x.ModId.Equals(modId, StringComparison.OrdinalIgnoreCase) &&
            x.ModVersion.Equals(modVersion, StringComparison.OrdinalIgnoreCase) &&
            x.GameId.Equals(gameId, StringComparison.OrdinalIgnoreCase) &&
            x.GameVersion.Equals(gameVersion, StringComparison.OrdinalIgnoreCase));
    }

    public void Upsert(CompatibilityRecord record)
    {
        var records = Load().Where(x =>
            !x.ModId.Equals(record.ModId, StringComparison.OrdinalIgnoreCase) ||
            !x.ModVersion.Equals(record.ModVersion, StringComparison.OrdinalIgnoreCase) ||
            !x.GameId.Equals(record.GameId, StringComparison.OrdinalIgnoreCase) ||
            !x.GameVersion.Equals(record.GameVersion, StringComparison.OrdinalIgnoreCase)).ToList();
        records.Add(record);
        Directory.CreateDirectory(Path.GetDirectoryName(_path) ?? ".");
        File.WriteAllText(_path, JsonSerializer.Serialize(records, JsonOptions));
    }

    private List<CompatibilityRecord> Load()
    {
        if (!File.Exists(_path))
        {
            return [];
        }

        return JsonSerializer.Deserialize<List<CompatibilityRecord>>(File.ReadAllText(_path), JsonOptions) ?? [];
    }
}
