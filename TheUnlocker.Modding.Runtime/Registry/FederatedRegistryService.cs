using System.Net.Http.Json;
using TheUnlocker.Configuration;
using TheUnlocker.Modding;

namespace TheUnlocker.Registry;

public sealed class FederatedRegistryPolicy
{
    public string RegistryName { get; init; } = "";
    public bool AllowUnsignedPackages { get; init; } = true;
    public string[] AllowedPublishers { get; init; } = [];
    public string[] BlockedMods { get; init; } = [];
    public string RequiredTrustLevel { get; init; } = "";
}

public sealed class FederatedSearchResult
{
    public string RegistryName { get; init; } = "";
    public string RegistryUrl { get; init; } = "";
    public ModRepositoryEntry Entry { get; init; } = new();
    public bool AllowedByPolicy { get; init; }
    public string PolicyDecision { get; init; } = "";
}

public sealed class FederatedRegistryService
{
    private readonly HttpClient _httpClient;

    public FederatedRegistryService(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
    }

    public async Task<IReadOnlyList<FederatedSearchResult>> SearchAsync(
        IEnumerable<FederatedRegistryEndpoint> registries,
        string query,
        IEnumerable<FederatedRegistryPolicy> policies,
        CancellationToken cancellationToken = default)
    {
        var policyByRegistry = policies.ToDictionary(policy => policy.RegistryName, StringComparer.OrdinalIgnoreCase);
        var results = new List<FederatedSearchResult>();

        foreach (var registry in registries.Where(item => !string.IsNullOrWhiteSpace(item.BaseUrl)))
        {
            var url = $"{registry.BaseUrl.TrimEnd('/')}/mods?q={Uri.EscapeDataString(query)}";
            ModRepositoryIndex? index;
            try
            {
                index = await _httpClient.GetFromJsonAsync<ModRepositoryIndex>(url, cancellationToken);
            }
            catch
            {
                continue;
            }

            if (index is null)
            {
                continue;
            }

            policyByRegistry.TryGetValue(registry.Name, out var policy);
            foreach (var entry in index.Mods)
            {
                var decision = EvaluatePolicy(entry, policy);
                results.Add(new FederatedSearchResult
                {
                    RegistryName = registry.Name,
                    RegistryUrl = registry.BaseUrl,
                    Entry = entry,
                    AllowedByPolicy = decision.Allowed,
                    PolicyDecision = decision.Reason
                });
            }
        }

        return results.OrderByDescending(result => result.AllowedByPolicy)
            .ThenBy(result => result.RegistryName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(result => result.Entry.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public IReadOnlyList<FederatedRegistryEndpoint> BuildRegistryList(LocalAppConfig config, params FederatedRegistryEndpoint[] defaults)
    {
        var registries = defaults.ToList();
        foreach (var url in config.Policy.PrivateRegistryUrls.Where(url => !string.IsNullOrWhiteSpace(url)))
        {
            registries.Add(new FederatedRegistryEndpoint
            {
                Name = new Uri(url).Host,
                BaseUrl = url,
                TrustPolicy = "private"
            });
        }

        return registries
            .GroupBy(item => item.BaseUrl.TrimEnd('/'), StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToArray();
    }

    private static (bool Allowed, string Reason) EvaluatePolicy(ModRepositoryEntry entry, FederatedRegistryPolicy? policy)
    {
        if (policy is null)
        {
            return (true, "No registry-specific policy.");
        }

        if (policy.BlockedMods.Contains(entry.Id, StringComparer.OrdinalIgnoreCase))
        {
            return (false, "Blocked by registry policy.");
        }

        if (policy.AllowedPublishers.Length > 0 && !policy.AllowedPublishers.Contains(entry.Author(), StringComparer.OrdinalIgnoreCase))
        {
            return (false, "Publisher is not allowlisted for this registry.");
        }

        return (true, "Allowed by registry policy.");
    }
}

internal static class ModRepositoryEntryExtensions
{
    public static string Author(this ModRepositoryEntry entry)
    {
        return entry.Id.Contains('/', StringComparison.Ordinal) ? entry.Id.Split('/')[0] : "";
    }
}
