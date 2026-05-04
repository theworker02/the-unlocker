using System.IO;
using System.Security.Cryptography;
using System.Text.Json;

namespace TheUnlocker.Modding;

public sealed class LocalPackageRegistry
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };
    private readonly string _registryDirectory;
    private readonly string _indexPath;

    public LocalPackageRegistry(string registryDirectory)
    {
        _registryDirectory = registryDirectory;
        _indexPath = Path.Combine(registryDirectory, "registry.json");
    }

    public IReadOnlyCollection<ModRegistryEntry> Entries => Load();

    public string CachePackage(string packagePath, string source)
    {
        Directory.CreateDirectory(Path.Combine(_registryDirectory, "packages"));
        var hash = ComputeSha256(packagePath);
        var target = Path.Combine(_registryDirectory, "packages", $"{Path.GetFileNameWithoutExtension(packagePath)}-{hash[..8]}{Path.GetExtension(packagePath)}");
        File.Copy(packagePath, target, overwrite: true);
        return target;
    }

    public void Record(ModManifest manifest, string packagePath, string source)
    {
        var entries = Load().ToList();
        entries.Add(new ModRegistryEntry
        {
            Id = manifest.Id,
            Version = manifest.Version,
            PackagePath = packagePath,
            Source = source,
            Sha256 = File.Exists(packagePath) ? ComputeSha256(packagePath) : "",
            InstalledAt = DateTimeOffset.Now
        });
        Save(entries);
    }

    private List<ModRegistryEntry> Load()
    {
        if (!File.Exists(_indexPath))
        {
            return new List<ModRegistryEntry>();
        }

        return JsonSerializer.Deserialize<List<ModRegistryEntry>>(File.ReadAllText(_indexPath), JsonOptions) ?? new List<ModRegistryEntry>();
    }

    private void Save(List<ModRegistryEntry> entries)
    {
        Directory.CreateDirectory(_registryDirectory);
        File.WriteAllText(_indexPath, JsonSerializer.Serialize(entries, JsonOptions));
    }

    private static string ComputeSha256(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream));
    }
}
