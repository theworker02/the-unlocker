using TheUnlocker.Sync;
using TheUnlocker.RegistryClient;

namespace TheUnlocker.Desktop;

public sealed class DesktopAccountSyncService
{
    private readonly ApiClient _apiClient;

    public DesktopAccountSyncService(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<DesktopAccountSyncResult> SignInAndSyncAsync(
        string apiBaseUrl,
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        var session = await _apiClient.SignInAsync(apiBaseUrl, email, password, cancellationToken);
        var sync = await _apiClient.GetSyncStateAsync(apiBaseUrl, session.User.Id, cancellationToken);

        return new DesktopAccountSyncResult
        {
            Session = session,
            SyncState = sync,
            Message = $"Signed in as {session.User.DisplayName}. Synced {sync.InstalledMods.Length} installed mods and {sync.Profiles.Count} profiles."
        };
    }
}

public sealed class DesktopAccountSyncResult
{
    public RegistrySession Session { get; init; } = new();
    public UserSyncState SyncState { get; init; } = new();
    public string Message { get; init; } = "";
}
