using System.IO;

namespace TheUnlocker.Modding;

public sealed class ModManifestValidator
{
    private static readonly HashSet<string> KnownSettingTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "text",
        "boolean",
        "number",
        "select"
    };

    public ManifestValidationResult Validate(
        ModManifest manifest,
        string modDirectory,
        IReadOnlyCollection<string> allModIds)
    {
        var result = new ManifestValidationResult();

        Require(result, manifest.Id, "id");
        Require(result, manifest.Name, "name");
        Require(result, manifest.Version, "version");
        Require(result, manifest.EntryDll, "entryDll");

        if (!string.IsNullOrWhiteSpace(manifest.Version) && !Version.TryParse(manifest.Version, out _))
        {
            result.Errors.Add("version must be a valid semantic version.");
        }

        if (!string.IsNullOrWhiteSpace(manifest.MinimumAppVersion) && !Version.TryParse(manifest.MinimumAppVersion, out _))
        {
            result.Errors.Add("minimumAppVersion must be a valid version.");
        }

        if (!string.IsNullOrWhiteSpace(manifest.MinimumFrameworkVersion) && !Version.TryParse(manifest.MinimumFrameworkVersion, out _))
        {
            result.Errors.Add("minimumFrameworkVersion must be a valid version.");
        }

        if (!string.IsNullOrWhiteSpace(manifest.EntryDll))
        {
            var entryDllPath = Path.Combine(modDirectory, manifest.EntryDll);
            if (!File.Exists(entryDllPath))
            {
                result.Errors.Add($"entryDll does not exist: {manifest.EntryDll}");
            }
        }

        foreach (var permission in manifest.Permissions)
        {
            if (!ModPermission.Known.Contains(permission))
            {
                result.Errors.Add($"Unknown permission: {permission}");
            }
        }

        foreach (var dependencyId in manifest.DependsOn)
        {
            if (!allModIds.Contains(dependencyId, StringComparer.OrdinalIgnoreCase))
            {
                result.Warnings.Add($"Dependency is not currently installed: {dependencyId}");
            }
        }

        foreach (var setting in manifest.Settings)
        {
            if (!KnownSettingTypes.Contains(setting.Value.Type))
            {
                result.Errors.Add($"Setting '{setting.Key}' has unknown type '{setting.Value.Type}'.");
            }

            if (setting.Value.Type.Equals("select", StringComparison.OrdinalIgnoreCase) && setting.Value.Options.Length == 0)
            {
                result.Errors.Add($"Select setting '{setting.Key}' must define options.");
            }
        }

        return result;
    }

    public ManifestValidationResult ValidateAll(IReadOnlyCollection<ModDiscoveryInfo> mods)
    {
        var result = new ManifestValidationResult();
        var duplicateIds = mods.GroupBy(mod => mod.Manifest.Id, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key);

        foreach (var duplicateId in duplicateIds)
        {
            result.Errors.Add($"Duplicate mod id: {duplicateId}");
        }

        var allIds = mods.Select(mod => mod.Manifest.Id).ToArray();
        foreach (var mod in mods)
        {
            var modResult = Validate(mod.Manifest, mod.DirectoryPath, allIds);
            result.Errors.AddRange(modResult.Errors.Select(error => $"{mod.Manifest.Id}: {error}"));
            result.Warnings.AddRange(modResult.Warnings.Select(warning => $"{mod.Manifest.Id}: {warning}"));
        }

        foreach (var cycle in FindDependencyCycles(mods))
        {
            result.Errors.Add($"Dependency cycle detected: {cycle}");
        }

        return result;
    }

    private static IEnumerable<string> FindDependencyCycles(IReadOnlyCollection<ModDiscoveryInfo> mods)
    {
        var byId = mods.ToDictionary(mod => mod.Manifest.Id, StringComparer.OrdinalIgnoreCase);
        var visiting = new Stack<string>();
        var temporary = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var permanent = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var cycles = new List<string>();

        foreach (var mod in mods)
        {
            Visit(mod.Manifest.Id);
        }

        return cycles;

        void Visit(string modId)
        {
            if (permanent.Contains(modId) || !byId.TryGetValue(modId, out var mod))
            {
                return;
            }

            if (!temporary.Add(modId))
            {
                cycles.Add(string.Join(" -> ", visiting.Reverse().Append(modId)));
                return;
            }

            visiting.Push(modId);
            foreach (var dependencyId in mod.Manifest.DependsOn)
            {
                Visit(dependencyId);
            }

            visiting.Pop();
            temporary.Remove(modId);
            permanent.Add(modId);
        }
    }

    private static void Require(ManifestValidationResult result, string? value, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            result.Errors.Add($"Missing required field: {field}");
        }
    }
}
