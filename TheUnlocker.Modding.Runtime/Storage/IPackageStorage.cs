namespace TheUnlocker.Storage;

public sealed class StoredPackage
{
    public string Key { get; init; } = "";
    public string Url { get; init; } = "";
    public string Sha256 { get; init; } = "";
    public long SizeBytes { get; init; }
}

public interface IPackageStorage
{
    Task<StoredPackage> PutAsync(string key, Stream package, CancellationToken cancellationToken = default);
    Task<Stream> GetAsync(string key, CancellationToken cancellationToken = default);
}
