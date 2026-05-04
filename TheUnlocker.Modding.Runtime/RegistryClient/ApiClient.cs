using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using TheUnlocker.Modding;
using TheUnlocker.Sync;

namespace TheUnlocker.RegistryClient;

public sealed class ApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true, WriteIndented = true };
    private readonly HttpClient _httpClient = new();
    private readonly AuthService _auth;

    public ApiClient(AuthService auth)
    {
        _auth = auth;
    }

    public async Task<ModRepositoryIndex> GetIndexAsync(string registryUrl, CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Get, registryUrl);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<ModRepositoryIndex>(json, JsonOptions) ?? new ModRepositoryIndex();
    }

    public async Task PublishAsync(string registryUrl, ModRepositoryEntry entry, CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Post, registryUrl.TrimEnd('/') + "/mods");
        request.Content = new StringContent(JsonSerializer.Serialize(entry, JsonOptions), Encoding.UTF8, "application/json");
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<RegistrySession> SignInAsync(string apiBaseUrl, string email, string password, CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Post, apiBaseUrl.TrimEnd('/') + "/api/v1/auth/login");
        request.Content = new StringContent(JsonSerializer.Serialize(new { email, password }, JsonOptions), Encoding.UTF8, "application/json");
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var session = JsonSerializer.Deserialize<RegistrySession>(json, JsonOptions) ?? new RegistrySession();
        _auth.UseToken(session.Token, session.RefreshToken);
        return session;
    }

    public async Task<RegistrySession> GetCurrentSessionAsync(string apiBaseUrl, CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Get, apiBaseUrl.TrimEnd('/') + "/api/v1/me");
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<RegistrySession>(json, JsonOptions) ?? new RegistrySession();
    }

    public async Task<UserSyncState> GetSyncStateAsync(string apiBaseUrl, string userId, CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Get, apiBaseUrl.TrimEnd('/') + "/api/v1/sync/" + Uri.EscapeDataString(userId));
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<UserSyncState>(json, JsonOptions) ?? new UserSyncState { UserId = userId };
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string url)
    {
        var request = new HttpRequestMessage(method, url);
        if (!string.IsNullOrWhiteSpace(_auth.Token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _auth.Token);
        }

        return request;
    }
}

public sealed class RegistrySession
{
    public string Token { get; init; } = "";
    public string RefreshToken { get; init; } = "";
    public DateTimeOffset ExpiresAt { get; init; }
    public DateTimeOffset RefreshExpiresAt { get; init; }
    public RegistryUser User { get; init; } = new();
}

public sealed class RegistryUser
{
    public string Id { get; init; } = "";
    public string Email { get; init; } = "";
    public string DisplayName { get; init; } = "";
    public string PrimaryGame { get; init; } = "";
    public string RegistryUrl { get; init; } = "";
}
