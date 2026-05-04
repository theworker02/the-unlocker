using TheUnlocker.Modding;

namespace TheUnlocker.PackageManager;

public sealed class PackageCandidate
{
    public string Id { get; init; } = "";
    public string Version { get; init; } = "";
    public string GameVersionRange { get; init; } = "";
    public string SdkVersionRange { get; init; } = "";
    public ModDependency[] Dependencies { get; init; } = [];
    public ModDependency[] PeerDependencies { get; init; } = [];
    public string[] ConflictsWith { get; init; } = [];
}

public sealed class DependencySolveRequest
{
    public string[] RequestedModIds { get; init; } = [];
    public string GameVersion { get; init; } = "";
    public string SdkVersion { get; init; } = "";
    public PackageCandidate[] Candidates { get; init; } = [];
}

public sealed class DependencySolveResult
{
    public bool Success => Errors.Length == 0;
    public PackageCandidate[] Selected { get; init; } = [];
    public string[] Errors { get; init; } = [];
    public Dictionary<string, string> Lockfile { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class SatDependencySolver
{
    public DependencySolveResult Solve(DependencySolveRequest request)
    {
        var candidatesById = request.Candidates
            .Where(candidate => VersionComparer.Satisfies(request.GameVersion, candidate.GameVersionRange))
            .Where(candidate => VersionComparer.Satisfies(request.SdkVersion, candidate.SdkVersionRange))
            .GroupBy(candidate => candidate.Id, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.OrderByDescending(candidate => candidate.Version, VersionComparer.Instance).ToArray(),
                StringComparer.OrdinalIgnoreCase);

        var selected = new Dictionary<string, PackageCandidate>(StringComparer.OrdinalIgnoreCase);
        var errors = new List<string>();

        foreach (var modId in request.RequestedModIds)
        {
            Select(modId, "", false, candidatesById, selected, errors);
        }

        foreach (var candidate in selected.Values.ToArray())
        {
            foreach (var peer in candidate.PeerDependencies)
            {
                if (!selected.TryGetValue(peer.Id, out var installed))
                {
                    errors.Add($"{candidate.Id} expects peer {peer.Id} {peer.VersionRange}, but it is not selected.");
                    continue;
                }

                if (!VersionComparer.Satisfies(installed.Version, peer.VersionRange))
                {
                    errors.Add($"{candidate.Id} expects peer {peer.Id} {peer.VersionRange}, selected {installed.Version}.");
                }
            }

            foreach (var conflict in candidate.ConflictsWith)
            {
                if (selected.ContainsKey(conflict))
                {
                    errors.Add($"{candidate.Id} conflicts with selected package {conflict}.");
                }
            }
        }

        return new DependencySolveResult
        {
            Selected = selected.Values.OrderBy(item => item.Id, StringComparer.OrdinalIgnoreCase).ToArray(),
            Errors = errors.Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            Lockfile = selected.ToDictionary(pair => pair.Key, pair => pair.Value.Version, StringComparer.OrdinalIgnoreCase)
        };
    }

    private static void Select(
        string id,
        string versionRange,
        bool optional,
        IReadOnlyDictionary<string, PackageCandidate[]> candidatesById,
        IDictionary<string, PackageCandidate> selected,
        ICollection<string> errors)
    {
        if (selected.TryGetValue(id, out var existing))
        {
            if (!VersionComparer.Satisfies(existing.Version, versionRange))
            {
                errors.Add($"{id} selected as {existing.Version}, which does not satisfy {versionRange}.");
            }
            return;
        }

        if (!candidatesById.TryGetValue(id, out var candidates))
        {
            if (!optional)
            {
                errors.Add($"No candidate found for required package {id}.");
            }
            return;
        }

        var candidate = candidates.FirstOrDefault(item => VersionComparer.Satisfies(item.Version, versionRange));
        if (candidate is null)
        {
            if (!optional)
            {
                errors.Add($"No candidate for {id} satisfies {versionRange}.");
            }
            return;
        }

        selected[id] = candidate;
        foreach (var dependency in candidate.Dependencies)
        {
            Select(dependency.Id, dependency.VersionRange, dependency.Optional, candidatesById, selected, errors);
        }
    }
}
