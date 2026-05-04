using System.Security.Cryptography;

namespace TheUnlocker.Registry;

public sealed class ProvenanceAttestation
{
    public string PredicateType { get; init; } = "https://slsa.dev/provenance/v1";
    public string SubjectName { get; init; } = "";
    public string SubjectDigestSha256 { get; init; } = "";
    public string BuilderId { get; init; } = "";
    public string BuildType { get; init; } = "";
    public string InvocationId { get; init; } = "";
}

public sealed class SbomDocument
{
    public string PackageName { get; init; } = "";
    public string PackageVersion { get; init; } = "";
    public SbomComponent[] Components { get; init; } = [];
}

public sealed class SbomComponent
{
    public string Name { get; init; } = "";
    public string Version { get; init; } = "";
    public string Path { get; init; } = "";
    public string Sha256 { get; init; } = "";
}

public sealed class SbomGenerator
{
    public SbomDocument Generate(string packageName, string packageVersion, string packageDirectory)
    {
        if (!Directory.Exists(packageDirectory))
        {
            throw new DirectoryNotFoundException(packageDirectory);
        }

        return new SbomDocument
        {
            PackageName = packageName,
            PackageVersion = packageVersion,
            Components = Directory.EnumerateFiles(packageDirectory, "*", SearchOption.AllDirectories)
                .Where(path => path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
                    || path.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
                    || path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                .Select(path => new SbomComponent
                {
                    Name = Path.GetFileName(path),
                    Version = packageVersion,
                    Path = Path.GetRelativePath(packageDirectory, path),
                    Sha256 = HashFile(path)
                })
                .ToArray()
        };
    }

    private static string HashFile(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }
}
