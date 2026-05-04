using TheUnlocker.Modding;

namespace TheUnlocker.PackageManager;

public sealed class DependencyGraph
{
    private readonly Dictionary<string, ModManifest> _mods;

    public DependencyGraph(IEnumerable<ModManifest> manifests)
    {
        _mods = manifests.ToDictionary(manifest => manifest.Id, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<string> ResolveLoadOrder()
    {
        var ordered = new List<string>();
        var temporary = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var permanent = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var id in _mods.Keys.OrderBy(id => id))
        {
            Visit(id);
        }

        return ordered;

        void Visit(string id)
        {
            if (permanent.Contains(id) || !_mods.ContainsKey(id))
            {
                return;
            }

            if (!temporary.Add(id))
            {
                throw new InvalidOperationException($"Dependency cycle detected at {id}.");
            }

            foreach (var dependencyId in _mods[id].DependsOn)
            {
                Visit(dependencyId);
            }

            foreach (var dependency in _mods[id].Dependencies.Where(dependency => !dependency.Optional))
            {
                Visit(dependency.Id);
            }

            temporary.Remove(id);
            permanent.Add(id);
            ordered.Add(id);
        }
    }
}
