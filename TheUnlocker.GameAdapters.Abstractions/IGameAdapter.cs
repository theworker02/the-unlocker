namespace TheUnlocker.GameAdapters;

public interface IGameAdapter
{
    string Id { get; }

    string DisplayName { get; }

    bool CanHandle(string gameRoot);

    GameInspectionResult Inspect(string gameRoot);

    CompatibilityProbeResult Probe(ModpackProbeRequest request);
}

public sealed class GameInspectionResult
{
    public string GameId { get; init; } = "";
    public string GameVersion { get; init; } = "";
    public string Runtime { get; init; } = "";
    public string[] ModDirectories { get; init; } = [];
    public string[] Warnings { get; init; } = [];
}

public sealed class ModpackProbeRequest
{
    public string GameRoot { get; init; } = "";
    public string ModpackPath { get; init; } = "";
    public string[] EnabledMods { get; init; } = [];
}

public sealed class CompatibilityProbeResult
{
    public bool Passed { get; init; }
    public string AdapterId { get; init; } = "";
    public TimeSpan Duration { get; init; }
    public string[] Warnings { get; init; } = [];
    public string[] Errors { get; init; } = [];
}
