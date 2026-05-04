using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;

namespace TheUnlocker.Workspaces;

public sealed class UnlockerWorkspace
{
    public string Name { get; init; } = "";
    public string GameId { get; init; } = "";
    public string ActiveProfile { get; init; } = "default";
    public Dictionary<string, string[]> Profiles { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class UnlockerLockFile
{
    public List<LockedModPackage> Mods { get; init; } = [];
}

public sealed class LockedModPackage
{
    public string Id { get; init; } = "";
    public string Version { get; init; } = "";
    public string Sha256 { get; init; } = "";
    public string Source { get; init; } = "";
}

public sealed class WorkspaceService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };

    public void Create(string directory, UnlockerWorkspace workspace)
    {
        Directory.CreateDirectory(Path.Combine(directory, "mods"));
        File.WriteAllText(Path.Combine(directory, "unlocker.json"), JsonSerializer.Serialize(workspace, JsonOptions));
        File.WriteAllText(Path.Combine(directory, "unlocker.lock.json"), JsonSerializer.Serialize(new UnlockerLockFile(), JsonOptions));
    }

    public UnlockerLockFile CreateLockFile(IEnumerable<string> packagePaths)
    {
        return new UnlockerLockFile
        {
            Mods = packagePaths.Select(path => new LockedModPackage
            {
                Id = Path.GetFileNameWithoutExtension(path),
                Version = "unknown",
                Sha256 = ComputeSha256(path),
                Source = path
            }).ToList()
        };
    }

    public string ExportModpack(string workspaceDirectory, string outputZipPath)
    {
        if (File.Exists(outputZipPath))
        {
            File.Delete(outputZipPath);
        }

        ZipFile.CreateFromDirectory(workspaceDirectory, outputZipPath);
        return outputZipPath;
    }

    public string ImportModpack(string modpackZipPath, string targetDirectory)
    {
        if (Directory.Exists(targetDirectory))
        {
            Directory.Delete(targetDirectory, recursive: true);
        }

        ZipFile.ExtractToDirectory(modpackZipPath, targetDirectory);
        return targetDirectory;
    }

    private static string ComputeSha256(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream));
    }
}
