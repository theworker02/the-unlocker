using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using TheUnlocker.Modding;
using TheUnlocker.Scanning;

namespace TheUnlocker.Registry;

public sealed class PackageScanReport
{
    public string PackagePath { get; init; } = "";
    public bool ManifestValid { get; init; }
    public string[] ValidationErrors { get; init; } = [];
    public Dictionary<string, string> FileHashes { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public MalwareScanResult[] ScanResults { get; init; } = [];
    public PackageReputationScore Reputation { get; init; } = new();
}

public sealed class PackageScanningPipeline
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly IReadOnlyCollection<IMalwareScanner> _scanners;

    public PackageScanningPipeline(IEnumerable<IMalwareScanner> scanners)
    {
        _scanners = scanners.ToArray();
    }

    public async Task<PackageScanReport> ScanAsync(string packagePath, CancellationToken cancellationToken = default)
    {
        var temp = Path.Combine(Path.GetTempPath(), $"theunlocker-scan-{Guid.NewGuid():N}");
        Directory.CreateDirectory(temp);
        try
        {
            ZipFile.ExtractToDirectory(packagePath, temp);
            var manifestPath = Directory.EnumerateFiles(temp, "mod.json", SearchOption.AllDirectories).FirstOrDefault();
            ModManifest? manifest = null;
            ManifestValidationResult validation = new();
            validation.Errors.Add("Missing mod.json");
            if (manifestPath is not null)
            {
                manifest = JsonSerializer.Deserialize<ModManifest>(File.ReadAllText(manifestPath), JsonOptions);
                if (manifest is not null)
                {
                    validation = new ModManifestValidator().Validate(manifest, Path.GetDirectoryName(manifestPath) ?? temp, [manifest.Id]);
                }
            }

            var hashes = Directory.EnumerateFiles(temp, "*", SearchOption.AllDirectories)
                .ToDictionary(path => Path.GetRelativePath(temp, path), ComputeSha256, StringComparer.OrdinalIgnoreCase);
            var scanResults = new List<MalwareScanResult>();
            foreach (var scanner in _scanners)
            {
                scanResults.Add(await scanner.ScanAsync(packagePath, cancellationToken));
            }

            var reputation = manifest is null
                ? new PackageReputationScore { ModId = Path.GetFileNameWithoutExtension(packagePath), Score = 0, Factors = ["Missing manifest"] }
                : new PackageReputationService().Score(manifest, ModSignatureStatus.Unsigned, scanResults.All(x => x.IsClean), validation.Errors.Count, 0, 0);

            return new PackageScanReport
            {
                PackagePath = packagePath,
                ManifestValid = validation.IsValid,
                ValidationErrors = validation.Errors.ToArray(),
                FileHashes = hashes,
                ScanResults = scanResults.ToArray(),
                Reputation = reputation
            };
        }
        finally
        {
            if (Directory.Exists(temp))
            {
                Directory.Delete(temp, recursive: true);
            }
        }
    }

    private static string ComputeSha256(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream));
    }
}
