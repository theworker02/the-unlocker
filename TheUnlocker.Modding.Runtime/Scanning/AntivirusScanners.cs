using System.Diagnostics;

namespace TheUnlocker.Scanning;

public sealed class ClamAvScanner : IMalwareScanner
{
    private readonly string _clamscanPath;

    public ClamAvScanner(string clamscanPath = "clamscan")
    {
        _clamscanPath = clamscanPath;
    }

    public async Task<MalwareScanResult> ScanAsync(string packagePath, CancellationToken cancellationToken = default)
    {
        return await RunScannerAsync("ClamAV", _clamscanPath, $"--no-summary \"{packagePath}\"", cancellationToken);
    }

    internal static async Task<MalwareScanResult> RunScannerAsync(string name, string fileName, string arguments, CancellationToken cancellationToken)
    {
        var process = Process.Start(new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        });

        if (process is null)
        {
            return new MalwareScanResult { IsClean = false, ScannerName = name, Findings = ["Scanner could not be started."] };
        }

        await process.WaitForExitAsync(cancellationToken);
        var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        var error = await process.StandardError.ReadToEndAsync(cancellationToken);
        return new MalwareScanResult
        {
            IsClean = process.ExitCode == 0,
            ScannerName = name,
            Findings = string.IsNullOrWhiteSpace(output + error) ? [] : (output + Environment.NewLine + error).Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
        };
    }
}

public sealed class YaraScanner : IMalwareScanner
{
    private readonly string _yaraPath;
    private readonly string _rulesPath;

    public YaraScanner(string rulesPath, string yaraPath = "yara")
    {
        _rulesPath = rulesPath;
        _yaraPath = yaraPath;
    }

    public async Task<MalwareScanResult> ScanAsync(string packagePath, CancellationToken cancellationToken = default)
    {
        var result = await ClamAvScanner.RunScannerAsync("YARA", _yaraPath, $"\"{_rulesPath}\" \"{packagePath}\"", cancellationToken);
        return new MalwareScanResult
        {
            IsClean = result.Findings.Length == 0,
            ScannerName = result.ScannerName,
            Findings = result.Findings
        };
    }
}
