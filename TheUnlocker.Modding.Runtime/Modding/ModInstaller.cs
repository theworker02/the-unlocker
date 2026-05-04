using System.IO;
using System.IO.Compression;
using System.Text.Json;

namespace TheUnlocker.Modding;

public sealed class ModInstaller
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private readonly string _modsDirectory;
    private readonly string _quarantineDirectory;
    private readonly string _stagingDirectory;
    private readonly LocalPackageRegistry _registry;
    private readonly ModManifestValidator _validator = new();

    public ModInstaller(string modsDirectory, string quarantineDirectory)
    {
        _modsDirectory = modsDirectory;
        _quarantineDirectory = quarantineDirectory;
        var root = Path.GetDirectoryName(modsDirectory) ?? modsDirectory;
        _stagingDirectory = Path.Combine(root, "Staging");
        _registry = new LocalPackageRegistry(Path.Combine(root, "Registry"));
    }

    public string Install(string sourcePath)
    {
        Directory.CreateDirectory(_modsDirectory);

        var extension = Path.GetExtension(sourcePath);
        if (extension.Equals(".zip", StringComparison.OrdinalIgnoreCase))
        {
            return InstallZip(sourcePath);
        }

        if (extension.Equals(".dll", StringComparison.OrdinalIgnoreCase))
        {
            return InstallDll(sourcePath);
        }

        throw new InvalidOperationException("Only .zip and .dll mod packages can be imported.");
    }

    private string InstallDll(string dllPath)
    {
        var id = Path.GetFileNameWithoutExtension(dllPath);
        var targetDirectory = Path.Combine(_modsDirectory, id);
        var backupDirectory = BackupExisting(targetDirectory);

        try
        {
            Directory.CreateDirectory(targetDirectory);

            var targetDll = Path.Combine(targetDirectory, Path.GetFileName(dllPath));
            File.Copy(dllPath, targetDll, overwrite: true);

            var manifest = new ModManifest
            {
                Id = id,
                Name = id,
                Version = "1.0.0",
                EntryDll = Path.GetFileName(dllPath)
            };

            File.WriteAllText(Path.Combine(targetDirectory, "mod.json"), JsonSerializer.Serialize(manifest, JsonOptions));
            ValidateOrThrow(targetDirectory);
            _registry.Record(manifest, targetDll, "local-dll");
            DeleteBackup(backupDirectory);
            return $"Installed DLL mod to {targetDirectory}.";
        }
        catch
        {
            Rollback(targetDirectory, backupDirectory);
            QuarantineSource(dllPath);
            throw;
        }
    }

    private string InstallZip(string zipPath)
    {
        var cachedPackage = _registry.CachePackage(zipPath, zipPath);
        var stagingDirectory = Path.Combine(_stagingDirectory, $".install-{Guid.NewGuid():N}");
        Directory.CreateDirectory(stagingDirectory);

        try
        {
            ZipFile.ExtractToDirectory(zipPath, stagingDirectory);
            var manifestPath = Directory.EnumerateFiles(stagingDirectory, "mod.json", SearchOption.AllDirectories).FirstOrDefault()
                ?? throw new InvalidOperationException("The package does not contain a mod.json manifest.");

            var manifest = JsonSerializer.Deserialize<ModManifest>(File.ReadAllText(manifestPath), JsonOptions)
                ?? throw new InvalidOperationException("The mod.json manifest could not be read.");

            if (string.IsNullOrWhiteSpace(manifest.Id) || string.IsNullOrWhiteSpace(manifest.EntryDll))
            {
                throw new InvalidOperationException("The mod manifest must include id and entryDll.");
            }

            var packageRoot = Path.GetDirectoryName(manifestPath)!;
            var entryDll = Path.Combine(packageRoot, manifest.EntryDll);
            if (!File.Exists(entryDll))
            {
                throw new InvalidOperationException($"The package is missing its entry DLL: {manifest.EntryDll}");
            }

            var targetDirectory = Path.Combine(_modsDirectory, manifest.Id);
            var backupDirectory = BackupExisting(targetDirectory);

            try
            {
                if (Directory.Exists(targetDirectory))
                {
                    Directory.Delete(targetDirectory, recursive: true);
                }

                Directory.Move(packageRoot, targetDirectory);
                ValidateOrThrow(targetDirectory);
                _registry.Record(manifest, cachedPackage, zipPath);
                DeleteBackup(backupDirectory);
                return $"Installed {manifest.Name} to {targetDirectory}.";
            }
            catch
            {
                Rollback(targetDirectory, backupDirectory);
                QuarantineSource(zipPath);
                throw;
            }
        }
        finally
        {
            if (Directory.Exists(stagingDirectory))
            {
                Directory.Delete(stagingDirectory, recursive: true);
            }
        }
    }

    private void ValidateOrThrow(string targetDirectory)
    {
        var manifestPath = Path.Combine(targetDirectory, "mod.json");
        var manifest = JsonSerializer.Deserialize<ModManifest>(File.ReadAllText(manifestPath), JsonOptions)
            ?? throw new InvalidOperationException("The mod manifest could not be read after install.");

        var result = _validator.Validate(manifest, targetDirectory, [manifest.Id]);
        if (!result.IsValid)
        {
            throw new InvalidOperationException(string.Join("; ", result.Errors));
        }
    }

    private static string? BackupExisting(string targetDirectory)
    {
        if (!Directory.Exists(targetDirectory))
        {
            return null;
        }

        var backupDirectory = $"{targetDirectory}.backup-{Guid.NewGuid():N}";
        Directory.Move(targetDirectory, backupDirectory);
        return backupDirectory;
    }

    private static void Rollback(string targetDirectory, string? backupDirectory)
    {
        if (Directory.Exists(targetDirectory))
        {
            Directory.Delete(targetDirectory, recursive: true);
        }

        if (backupDirectory is not null && Directory.Exists(backupDirectory))
        {
            Directory.Move(backupDirectory, targetDirectory);
        }
    }

    private static void DeleteBackup(string? backupDirectory)
    {
        if (backupDirectory is not null && Directory.Exists(backupDirectory))
        {
            Directory.Delete(backupDirectory, recursive: true);
        }
    }

    private void QuarantineSource(string sourcePath)
    {
        Directory.CreateDirectory(_quarantineDirectory);
        var target = Path.Combine(
            _quarantineDirectory,
            $"{Path.GetFileNameWithoutExtension(sourcePath)}-{Guid.NewGuid():N}{Path.GetExtension(sourcePath)}");
        File.Copy(sourcePath, target, overwrite: true);
    }
}
