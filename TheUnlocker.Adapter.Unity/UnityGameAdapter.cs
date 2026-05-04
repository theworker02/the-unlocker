using TheUnlocker.Adapters;

namespace TheUnlocker.Adapter.Unity;

public sealed class UnityGameAdapter : IGameAdapter
{
    public string Id => "unity";
    public string Name => "Unity";

    public IReadOnlyCollection<DetectedGame> DetectGames(IEnumerable<string> candidateRoots)
    {
        return candidateRoots
            .Where(Directory.Exists)
            .SelectMany(root => Directory.EnumerateFiles(root, "Assembly-CSharp.dll", SearchOption.AllDirectories))
            .Select(path => new DetectedGame
            {
                Id = Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(path)) ?? path),
                Name = Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(path)) ?? path),
                Path = Path.GetDirectoryName(Path.GetDirectoryName(path)) ?? path,
                AdapterId = Id
            })
            .DistinctBy(x => x.Path, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public bool CanManage(string gamePath)
    {
        return Directory.Exists(gamePath) &&
            Directory.EnumerateFiles(gamePath, "Assembly-CSharp.dll", SearchOption.AllDirectories).Any();
    }
}
