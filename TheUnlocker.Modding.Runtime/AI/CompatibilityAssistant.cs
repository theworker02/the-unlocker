using TheUnlocker.Modding;

namespace TheUnlocker.AI;

public sealed class CompatibilityAssistantFinding
{
    public string Severity { get; init; } = "Info";
    public string Message { get; init; } = "";
    public string SuggestedAction { get; init; } = "";
}

public sealed class CompatibilityAssistant
{
    public IReadOnlyList<CompatibilityAssistantFinding> Analyze(
        IEnumerable<ModManifest> manifests,
        IEnumerable<string> logs,
        IEnumerable<string> crashStackLines)
    {
        var findings = new List<CompatibilityAssistantFinding>();
        var manifestArray = manifests.ToArray();
        foreach (var manifest in manifestArray)
        {
            foreach (var dependency in manifest.Dependencies.Where(dep => !dep.Optional))
            {
                if (!manifestArray.Any(item => item.Id.Equals(dependency.Id, StringComparison.OrdinalIgnoreCase)))
                {
                    findings.Add(new CompatibilityAssistantFinding
                    {
                        Severity = "Error",
                        Message = $"{manifest.Id} is missing dependency {dependency.Id}.",
                        SuggestedAction = $"Install {dependency.Id} before enabling {manifest.Id}."
                    });
                }
            }
        }

        foreach (var targetGroup in manifestArray.SelectMany(mod => mod.Targets.Select(target => new { mod.Id, Target = target }))
                     .GroupBy(item => item.Target, StringComparer.OrdinalIgnoreCase)
                     .Where(group => group.Count() > 1))
        {
            findings.Add(new CompatibilityAssistantFinding
            {
                Severity = "Warning",
                Message = $"Multiple mods target {targetGroup.Key}: {string.Join(", ", targetGroup.Select(item => item.Id))}.",
                SuggestedAction = "Review load order or install a compatibility patch."
            });
        }

        if (logs.Concat(crashStackLines).Any(line => line.Contains("permission", StringComparison.OrdinalIgnoreCase)))
        {
            findings.Add(new CompatibilityAssistantFinding
            {
                Severity = "Warning",
                Message = "Logs mention permission failures.",
                SuggestedAction = "Compare requested permissions against the active policy."
            });
        }

        return findings;
    }
}
