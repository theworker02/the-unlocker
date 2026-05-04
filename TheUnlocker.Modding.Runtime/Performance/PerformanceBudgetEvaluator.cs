using TheUnlocker.Modding;

namespace TheUnlocker.Performance;

public sealed class PerformanceBudget
{
    public TimeSpan MaxLoadTime { get; init; } = TimeSpan.FromSeconds(2);
    public long MaxMemoryDeltaBytes { get; init; } = 100 * 1024 * 1024;
    public int MaxCrashCount { get; init; } = 3;
}

public sealed class PerformanceBudgetResult
{
    public string ModId { get; init; } = "";
    public bool IsWithinBudget { get; init; }
    public string[] Warnings { get; init; } = [];
}

public sealed class PerformanceBudgetEvaluator
{
    public PerformanceBudgetResult Evaluate(ModHealthInfo health, PerformanceBudget budget)
    {
        var warnings = new List<string>();
        if (health.LoadTime > budget.MaxLoadTime)
        {
            warnings.Add($"Load time {health.LoadTime.TotalMilliseconds:0}ms exceeds {budget.MaxLoadTime.TotalMilliseconds:0}ms.");
        }

        if (health.MemoryDeltaBytes > budget.MaxMemoryDeltaBytes)
        {
            warnings.Add($"Memory delta {health.MemoryDeltaBytes} bytes exceeds {budget.MaxMemoryDeltaBytes} bytes.");
        }

        if (health.ExceptionsThrown > budget.MaxCrashCount)
        {
            warnings.Add($"Crash count {health.ExceptionsThrown} exceeds {budget.MaxCrashCount}.");
        }

        return new PerformanceBudgetResult
        {
            ModId = health.ModId,
            IsWithinBudget = warnings.Count == 0,
            Warnings = warnings.ToArray()
        };
    }
}
