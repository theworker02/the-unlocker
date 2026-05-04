namespace TheUnlocker.Sync;

public sealed class CloudDevice
{
    public string DeviceId { get; init; } = Guid.NewGuid().ToString("N");
    public string Name { get; init; } = Environment.MachineName;
    public string Platform { get; init; } = Environment.OSVersion.Platform.ToString();
    public DateTimeOffset LastSeenAt { get; init; } = DateTimeOffset.UtcNow;
}

public sealed class CloudProfileSnapshot
{
    public string UserId { get; init; } = "";
    public string DeviceId { get; init; } = "";
    public UserSyncState State { get; init; } = new();
    public string[] LoadOrder { get; init; } = [];
    public string[] TrustDecisions { get; init; } = [];
    public DateTimeOffset CapturedAt { get; init; } = DateTimeOffset.UtcNow;
}

public sealed class CloudProfileSyncService
{
    public CloudProfileSnapshot CreateSnapshot(string userId, CloudDevice device, UserSyncState state, IEnumerable<string> loadOrder, IEnumerable<string> trustDecisions)
    {
        return new CloudProfileSnapshot
        {
            UserId = userId,
            DeviceId = device.DeviceId,
            State = state,
            LoadOrder = loadOrder.ToArray(),
            TrustDecisions = trustDecisions.ToArray()
        };
    }

    public UserSyncState Merge(UserSyncState local, CloudProfileSnapshot remote)
    {
        return new UserSyncState
        {
            UserId = local.UserId,
            InstalledMods = local.InstalledMods.Concat(remote.State.InstalledMods).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            Favorites = local.Favorites.Concat(remote.State.Favorites).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            Profiles = local.Profiles.Concat(remote.State.Profiles)
                .GroupBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.SelectMany(pair => pair.Value).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(), StringComparer.OrdinalIgnoreCase),
            Ratings = local.Ratings.Concat(remote.State.Ratings)
                .GroupBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.Last().Value, StringComparer.OrdinalIgnoreCase)
        };
    }
}
