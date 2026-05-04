using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using TheUnlocker.Modding;
using TheUnlocker.Workspaces;

namespace TheUnlocker.Registry;

public sealed class PackageManifestLockValidator
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public PackageLockValidationResult ValidatePackage(string packagePath, UnlockerLockFile? lockFile = null)
    {
        var errors = new List<string>();
        var warnings = new List<string>();
        var temp = Path.Combine(Path.GetTempPath(), $"theunlocker-lockcheck-{Guid.NewGuid():N}");
        Directory.CreateDirectory(temp);
        try
        {
            ZipFile.ExtractToDirectory(packagePath, temp);
            var manifestPath = Directory.EnumerateFiles(temp, "mod.json", SearchOption.AllDirectories).FirstOrDefault();
            if (manifestPath is null)
            {
                errors.Add("Package is missing mod.json.");
                return new PackageLockValidationResult(false, errors.ToArray(), warnings.ToArray());
            }

            var manifest = JsonSerializer.Deserialize<ModManifest>(File.ReadAllText(manifestPath), JsonOptions);
            if (manifest is null)
            {
                errors.Add("mod.json could not be parsed.");
                return new PackageLockValidationResult(false, errors.ToArray(), warnings.ToArray());
            }

            var packageRoot = Path.GetDirectoryName(manifestPath) ?? temp;
            var entryDll = Path.Combine(packageRoot, manifest.EntryDll);
            if (string.IsNullOrWhiteSpace(manifest.EntryDll) || !File.Exists(entryDll))
            {
                errors.Add($"Declared entry DLL '{manifest.EntryDll}' was not found.");
            }

            foreach (var executable in Directory.EnumerateFiles(packageRoot, "*.*", SearchOption.AllDirectories)
                         .Where(path => Path.GetExtension(path).Equals(".exe", StringComparison.OrdinalIgnoreCase)))
            {
                warnings.Add($"Package contains executable payload: {Path.GetRelativePath(packageRoot, executable)}.");
            }

            if (manifest.Signature is not null && File.Exists(entryDll))
            {
                var hash = ComputeSha256(entryDll);
                if (!hash.Equals(manifest.Signature.Sha256, StringComparison.OrdinalIgnoreCase))
                {
                    errors.Add("Entry DLL hash does not match manifest signature metadata.");
                }
            }

            if (lockFile is not null)
            {
                var locked = lockFile.Mods.FirstOrDefault(x => x.Id.Equals(manifest.Id, StringComparison.OrdinalIgnoreCase));
                if (locked is null)
                {
                    warnings.Add($"Package '{manifest.Id}' is not present in the supplied lockfile.");
                }
                else if (!locked.Version.Equals(manifest.Version, StringComparison.OrdinalIgnoreCase))
                {
                    errors.Add($"Package version {manifest.Version} does not match lockfile version {locked.Version}.");
                }
            }

            return new PackageLockValidationResult(errors.Count == 0, errors.ToArray(), warnings.ToArray());
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

public sealed record PackageLockValidationResult(bool IsValid, string[] Errors, string[] Warnings);

public sealed class DeltaUpdatePlanner
{
    public DeltaUpdatePlan Create(IReadOnlyDictionary<string, string> oldHashes, IReadOnlyDictionary<string, string> newHashes)
    {
        var addedOrChanged = newHashes
            .Where(pair => !oldHashes.TryGetValue(pair.Key, out var oldHash) || !oldHash.Equals(pair.Value, StringComparison.OrdinalIgnoreCase))
            .Select(pair => pair.Key)
            .OrderBy(x => x)
            .ToArray();
        var removed = oldHashes.Keys
            .Where(path => !newHashes.ContainsKey(path))
            .OrderBy(x => x)
            .ToArray();
        return new DeltaUpdatePlan(addedOrChanged, removed);
    }
}

public sealed record DeltaUpdatePlan(string[] DownloadFiles, string[] RemoveFiles);

public sealed class ModerationScannerRule
{
    public string Id { get; init; } = "";
    public string Description { get; init; } = "";
    public string Severity { get; init; } = "Warning";
    public string[] MatchExtensions { get; init; } = [];
    public string[] MatchFileNames { get; init; } = [];
    public string[] MatchPermissions { get; init; } = [];
}

public sealed class ModerationScannerRuleSet
{
    public List<ModerationScannerRule> Rules { get; init; } =
    [
        new() { Id = "unexpected-exe", Description = "Unexpected executable payload", Severity = "High", MatchExtensions = [".exe"] },
        new() { Id = "network-heavy", Description = "Network permission requested", Severity = "Medium", MatchPermissions = ["Network"] },
        new() { Id = "unsafe-filesystem", Description = "Broad filesystem permission requested", Severity = "Medium", MatchPermissions = ["WriteFiles", "FileSystem"] },
        new() { Id = "packed-binary", Description = "Packed binary naming pattern", Severity = "High", MatchFileNames = ["packed", "obfuscated"] }
    ];
}

public sealed class ConflictPatchRecommendationEngine
{
    public CompatibilityPatchRecommendation[] Recommend(IEnumerable<ModManifest> manifests)
    {
        return manifests
            .SelectMany(left => manifests.Where(right => !ReferenceEquals(left, right)), (left, right) => new { left, right })
            .Where(pair => string.Compare(pair.left.Id, pair.right.Id, StringComparison.OrdinalIgnoreCase) < 0)
            .Where(pair => pair.left.Targets.Intersect(pair.right.Targets, StringComparer.OrdinalIgnoreCase).Any())
            .Select(pair => new CompatibilityPatchRecommendation
            {
                ModIds = [pair.left.Id, pair.right.Id],
                SharedTargets = pair.left.Targets.Intersect(pair.right.Targets, StringComparer.OrdinalIgnoreCase).ToArray(),
                Recommendation = $"Consider a bridge patch or load-order rule for {pair.left.Id} and {pair.right.Id}."
            })
            .ToArray();
    }
}

public sealed class CompatibilityPatchRecommendation
{
    public string[] ModIds { get; init; } = [];
    public string[] SharedTargets { get; init; } = [];
    public string Recommendation { get; init; } = "";
}
