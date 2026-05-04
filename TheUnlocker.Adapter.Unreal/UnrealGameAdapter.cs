using TheUnlocker.Adapters;

namespace TheUnlocker.Adapter.Unreal;

public sealed class UnrealGameAdapter : IGameAdapter
{
    public string Id => "unreal";
    public string Name => "Unreal";

    public IReadOnlyCollection<DetectedGame> DetectGames(IEnumerable<string> candidateRoots)
    {
        return candidateRoots
            .Where(Directory.Exists)
            .SelectMany(root => Directory.EnumerateFiles(root, "*.uproject", SearchOption.AllDirectories))
            .Select(path => new DetectedGame
            {
                Id = Path.GetFileNameWithoutExtension(path),
                Name = Path.GetFileNameWithoutExtension(path),
                Path = Path.GetDirectoryName(path) ?? path,
                AdapterId = Id
            })
            .ToList();
    }

    public bool CanManage(string gamePath)
    {
        return Directory.Exists(gamePath) &&
            Directory.EnumerateFiles(gamePath, "*.uproject", SearchOption.TopDirectoryOnly).Any();
    }
}
