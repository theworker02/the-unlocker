using System.Text.Json;

namespace TheUnlocker.LaunchProfiles;

public sealed class GameLaunchProfile
{
    public string Name { get; init; } = "";
    public string GameId { get; init; } = "";
    public string[] EnabledMods { get; init; } = [];
    public string[] Arguments { get; init; } = [];
    public Dictionary<string, string> Environment { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class LaunchProfileStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };
    private readonly string _path;

    public LaunchProfileStore(string path)
    {
        _path = path;
    }

    public IReadOnlyCollection<GameLaunchProfile> Load()
    {
        if (!File.Exists(_path))
        {
            return [];
        }

        return JsonSerializer.Deserialize<List<GameLaunchProfile>>(File.ReadAllText(_path), JsonOptions) ?? [];
    }

    public void Save(IEnumerable<GameLaunchProfile> profiles)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path) ?? ".");
        File.WriteAllText(_path, JsonSerializer.Serialize(profiles, JsonOptions));
    }
}
