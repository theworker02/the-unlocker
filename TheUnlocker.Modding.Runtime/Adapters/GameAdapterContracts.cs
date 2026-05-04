namespace TheUnlocker.Adapters;

public sealed class DetectedGame
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string Path { get; init; } = "";
    public string Version { get; init; } = "";
    public string AdapterId { get; init; } = "";
}

public interface IGameAdapter
{
    string Id { get; }
    string Name { get; }
    IReadOnlyCollection<DetectedGame> DetectGames(IEnumerable<string> candidateRoots);
    bool CanManage(string gamePath);
}
