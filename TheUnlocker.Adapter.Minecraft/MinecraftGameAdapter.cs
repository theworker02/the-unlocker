using TheUnlocker.Adapters;

namespace TheUnlocker.Adapter.Minecraft;

public sealed class MinecraftGameAdapter : IGameAdapter
{
    public string Id => "minecraft";
    public string Name => "Minecraft";

    public IReadOnlyCollection<DetectedGame> DetectGames(IEnumerable<string> candidateRoots)
    {
        return candidateRoots
            .Where(Directory.Exists)
            .Where(root => Directory.Exists(Path.Combine(root, "mods")) || Directory.Exists(Path.Combine(root, "versions")))
            .Select(root => new DetectedGame
            {
                Id = "minecraft",
                Name = "Minecraft",
                Path = root,
                AdapterId = Id
            })
            .ToList();
    }

    public bool CanManage(string gamePath)
    {
        return Directory.Exists(Path.Combine(gamePath, "mods")) || Directory.Exists(Path.Combine(gamePath, "versions"));
    }
}
