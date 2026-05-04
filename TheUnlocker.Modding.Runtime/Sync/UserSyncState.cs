using System.Text.Json;

namespace TheUnlocker.Sync;

public sealed class UserSyncState
{
    public string UserId { get; init; } = "";
    public string[] InstalledMods { get; init; } = [];
    public string[] Favorites { get; init; } = [];
    public Dictionary<string, string[]> Profiles { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, int> Ratings { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class UserSyncStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };
    private readonly string _directory;

    public UserSyncStore(string directory)
    {
        _directory = directory;
    }

    public UserSyncState Load(string userId)
    {
        var path = Path.Combine(_directory, $"{userId}.json");
        if (!File.Exists(path))
        {
            return new UserSyncState { UserId = userId };
        }

        return JsonSerializer.Deserialize<UserSyncState>(File.ReadAllText(path), JsonOptions) ?? new UserSyncState { UserId = userId };
    }

    public void Save(UserSyncState state)
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(Path.Combine(_directory, $"{state.UserId}.json"), JsonSerializer.Serialize(state, JsonOptions));
    }
}
