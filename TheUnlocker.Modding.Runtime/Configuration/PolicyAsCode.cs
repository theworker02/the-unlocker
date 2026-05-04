using System.Text.Json;

namespace TheUnlocker.Configuration;

public sealed class PolicyAsCodeDocument
{
    public bool AllowUnsignedMods { get; init; } = true;
    public string[] AllowedRegistries { get; init; } = [];
    public string[] BlockedPermissions { get; init; } = [];
    public string RequiredTrustLevel { get; init; } = "";
    public string[] BlockedMods { get; init; } = [];
}

public sealed class PolicyAsCodeService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };

    public PolicyAsCodeDocument Load(string path)
    {
        if (!File.Exists(path))
        {
            return new PolicyAsCodeDocument();
        }

        return JsonSerializer.Deserialize<PolicyAsCodeDocument>(File.ReadAllText(path), JsonOptions) ?? new PolicyAsCodeDocument();
    }

    public string Save(string path, PolicyAsCodeDocument document)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
        File.WriteAllText(path, JsonSerializer.Serialize(document, JsonOptions));
        return path;
    }

    public string[] Evaluate(PolicyAsCodeDocument policy, string registryUrl, IEnumerable<string> permissions, string trustLevel, string modId)
    {
        var problems = new List<string>();
        if (!policy.AllowUnsignedMods && trustLevel.Equals("Unknown", StringComparison.OrdinalIgnoreCase))
        {
            problems.Add("Unsigned or unknown-trust mods are blocked.");
        }

        if (policy.AllowedRegistries.Length > 0 && !policy.AllowedRegistries.Contains(registryUrl, StringComparer.OrdinalIgnoreCase))
        {
            problems.Add("Registry is not allowlisted.");
        }

        foreach (var blockedPermission in policy.BlockedPermissions)
        {
            if (permissions.Contains(blockedPermission, StringComparer.OrdinalIgnoreCase))
            {
                problems.Add($"Permission {blockedPermission} is blocked.");
            }
        }

        if (policy.BlockedMods.Contains(modId, StringComparer.OrdinalIgnoreCase))
        {
            problems.Add("Mod is blocked by policy.");
        }

        return problems.ToArray();
    }
}
