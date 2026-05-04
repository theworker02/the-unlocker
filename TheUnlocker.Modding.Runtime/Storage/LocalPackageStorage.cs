using System.Security.Cryptography;

namespace TheUnlocker.Storage;

public sealed class LocalPackageStorage : IPackageStorage
{
    private readonly string _root;
    private readonly string _publicBaseUrl;

    public LocalPackageStorage(string root, string publicBaseUrl = "/packages")
    {
        _root = root;
        _publicBaseUrl = publicBaseUrl.TrimEnd('/');
    }

    public async Task<StoredPackage> PutAsync(string key, Stream package, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_root);
        var safeKey = key.Replace('\\', '/').Trim('/');
        var target = Path.Combine(_root, safeKey.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(target) ?? _root);

        await using var file = File.Create(target);
        await package.CopyToAsync(file, cancellationToken);
        await file.FlushAsync(cancellationToken);

        return new StoredPackage
        {
            Key = safeKey,
            Url = $"{_publicBaseUrl}/{Uri.EscapeDataString(safeKey)}",
            Sha256 = ComputeSha256(target),
            SizeBytes = new FileInfo(target).Length
        };
    }

    public Task<Stream> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        var target = Path.Combine(_root, key.Replace('/', Path.DirectorySeparatorChar));
        return Task.FromResult<Stream>(File.OpenRead(target));
    }

    private static string ComputeSha256(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream));
    }
}
