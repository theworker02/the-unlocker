using TheUnlocker.Modding;

namespace TheUnlocker.PackageManager;

public sealed class Resolver
{
    public IReadOnlyList<string> Resolve(IEnumerable<ModManifest> manifests)
    {
        return new DependencyGraph(manifests).ResolveLoadOrder();
    }

    public IReadOnlyList<string> GetProblems(IEnumerable<ModManifest> manifests)
    {
        var mods = manifests.ToDictionary(manifest => manifest.Id, StringComparer.OrdinalIgnoreCase);
        var problems = new List<string>();

        foreach (var mod in mods.Values)
        {
            foreach (var dependencyId in mod.DependsOn)
            {
                if (!mods.ContainsKey(dependencyId))
                {
                    problems.Add($"{mod.Id} requires missing dependency {dependencyId}.");
                }
            }

            foreach (var dependency in mod.Dependencies)
            {
                if (!mods.TryGetValue(dependency.Id, out var installed))
                {
                    if (!dependency.Optional)
                    {
                        problems.Add($"{mod.Id} requires missing dependency {dependency.Id}.");
                    }

                    continue;
                }

                if (!VersionComparer.Satisfies(installed.Version, dependency.VersionRange))
                {
                    problems.Add($"{mod.Id} requires {dependency.Id} {dependency.VersionRange}, installed {installed.Version}.");
                }
            }
        }

        return problems;
    }
}
