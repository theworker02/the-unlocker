namespace TheUnlocker.Compatibility;

public sealed class CompatibilityLabJob
{
    public string JobId { get; init; } = Guid.NewGuid().ToString("N");
    public string AdapterId { get; init; } = "";
    public string GameVersion { get; init; } = "";
    public string[] ModIds { get; init; } = [];
    public string LockfilePath { get; init; } = "";
}

public sealed class CompatibilityLabResult
{
    public string JobId { get; init; } = "";
    public bool Passed { get; init; }
    public string[] Logs { get; init; } = [];
    public string[] SuspectedMods { get; init; } = [];
    public TimeSpan Duration { get; init; }
}

public sealed class CompatibilityLabService
{
    public CompatibilityLabJob Queue(string adapterId, string gameVersion, IEnumerable<string> modIds, string lockfilePath)
    {
        return new CompatibilityLabJob
        {
            AdapterId = adapterId,
            GameVersion = gameVersion,
            ModIds = modIds.ToArray(),
            LockfilePath = lockfilePath
        };
    }

    public CompatibilityLabResult Summarize(CompatibilityLabJob job, IEnumerable<string> logs)
    {
        var logArray = logs.ToArray();
        var failed = logArray.Any(line =>
            line.Contains("crash", StringComparison.OrdinalIgnoreCase) ||
            line.Contains("freeze", StringComparison.OrdinalIgnoreCase) ||
            line.Contains("exception", StringComparison.OrdinalIgnoreCase));

        return new CompatibilityLabResult
        {
            JobId = job.JobId,
            Passed = !failed,
            Logs = logArray,
            SuspectedMods = failed ? job.ModIds : [],
            Duration = TimeSpan.FromSeconds(Math.Max(1, logArray.Length))
        };
    }
}
