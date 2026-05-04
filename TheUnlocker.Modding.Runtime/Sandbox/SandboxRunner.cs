using System.Diagnostics;

namespace TheUnlocker.Sandbox;

public sealed class SandboxRunner
{
    public Process Start(string executablePath, string workingDirectory)
    {
        var sandboxDirectory = Path.Combine(Path.GetTempPath(), $"the-unlocker-sandbox-{Guid.NewGuid():N}");
        Directory.CreateDirectory(sandboxDirectory);

        return Process.Start(new ProcessStartInfo
        {
            FileName = executablePath,
            WorkingDirectory = Directory.Exists(workingDirectory) ? workingDirectory : sandboxDirectory,
            UseShellExecute = true
        }) ?? throw new InvalidOperationException("Could not start sandbox process.");
    }
}
