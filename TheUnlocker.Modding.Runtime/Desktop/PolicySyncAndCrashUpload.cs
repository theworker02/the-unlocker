using System.Net.Http.Json;
using TheUnlocker.Configuration;
using TheUnlocker.Registry;
using TheUnlocker.Wow;

namespace TheUnlocker.Desktop;

public sealed class PolicySyncService
{
    private readonly HttpClient _http = new();

    public async Task<EnterprisePolicy?> FetchAsync(string registryBaseUrl, string teamId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(registryBaseUrl) || string.IsNullOrWhiteSpace(teamId))
        {
            return null;
        }

        return await _http.GetFromJsonAsync<EnterprisePolicy>($"{registryBaseUrl.TrimEnd('/')}/enterprise/policy/{Uri.EscapeDataString(teamId)}", cancellationToken);
    }

    public void Apply(EnterprisePolicy policy, LocalAppConfig config)
    {
        foreach (var blocked in policy.BlockedMods)
        {
            config.Policy.BlockedMods.Add(blocked);
            config.EnabledMods.Remove(blocked);
        }
    }
}

public sealed class CrashUploadService
{
    private readonly HttpClient _http = new();

    public async Task<string> UploadAsync(string registryBaseUrl, string diagnosticsZipPath, string userId, CancellationToken cancellationToken = default)
    {
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(userId), "userId");
        form.Add(new StreamContent(File.OpenRead(diagnosticsZipPath)), "bundle", Path.GetFileName(diagnosticsZipPath));
        var response = await _http.PostAsync($"{registryBaseUrl.TrimEnd('/')}/crash-reports/upload", form, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }
}

public sealed class TrustSimulationService
{
    public string Simulate(string modId, IEnumerable<string> permissions, IEnumerable<string> targets)
    {
        return $"{modId} requests {string.Join(", ", permissions.DefaultIfEmpty("no permissions"))} and targets {string.Join(", ", targets.DefaultIfEmpty("no systems"))}.";
    }
}
