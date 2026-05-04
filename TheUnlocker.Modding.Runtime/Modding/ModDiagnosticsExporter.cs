using System.IO;
using System.IO.Compression;
using System.Text.Json;
using TheUnlocker.Configuration;

namespace TheUnlocker.Modding;

public static class ModDiagnosticsExporter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public static string Export(
        string outputDirectory,
        LocalAppConfig config,
        IReadOnlyCollection<string> logs,
        IReadOnlyCollection<ModHealthInfo> health,
        IEnumerable<string> manifestPaths)
    {
        Directory.CreateDirectory(outputDirectory);
        var staging = Path.Combine(Path.GetTempPath(), $"the-unlocker-diagnostics-{Guid.NewGuid():N}");
        Directory.CreateDirectory(staging);

        try
        {
            File.WriteAllText(Path.Combine(staging, "content-config.json"), JsonSerializer.Serialize(config, JsonOptions));
            File.WriteAllLines(Path.Combine(staging, "mod-loader.log"), logs);
            File.WriteAllText(Path.Combine(staging, "health.json"), JsonSerializer.Serialize(health, JsonOptions));

            var manifestsDirectory = Path.Combine(staging, "manifests");
            Directory.CreateDirectory(manifestsDirectory);
            foreach (var manifestPath in manifestPaths)
            {
                var name = $"{Path.GetFileName(Path.GetDirectoryName(manifestPath) ?? "mod")}-{Path.GetFileName(manifestPath)}";
                File.Copy(manifestPath, Path.Combine(manifestsDirectory, name), overwrite: true);
            }

            var errors = logs.Where(log => log.Contains("Error", StringComparison.OrdinalIgnoreCase)
                || log.Contains("failed", StringComparison.OrdinalIgnoreCase)
                || log.Contains("crashed", StringComparison.OrdinalIgnoreCase));
            File.WriteAllLines(Path.Combine(staging, "recent-errors.log"), errors);

            var zipPath = Path.Combine(outputDirectory, $"the-unlocker-diagnostics-{DateTimeOffset.Now:yyyyMMdd-HHmmss}.zip");
            if (File.Exists(zipPath))
            {
                File.Delete(zipPath);
            }

            ZipFile.CreateFromDirectory(staging, zipPath);
            return zipPath;
        }
        finally
        {
            if (Directory.Exists(staging))
            {
                Directory.Delete(staging, recursive: true);
            }
        }
    }
}
