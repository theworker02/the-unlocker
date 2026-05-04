namespace TheUnlocker.GameDetection;

public sealed class SteamScanner
{
    public IReadOnlyList<string> FindLibraries(IEnumerable<string> candidateRoots)
    {
        return candidateRoots
            .Select(root => Path.Combine(root, "steamapps"))
            .Where(Directory.Exists)
            .ToArray();
    }
}
