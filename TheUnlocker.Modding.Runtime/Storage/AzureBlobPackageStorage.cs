using System.Security.Cryptography;
using Azure.Storage.Blobs;

namespace TheUnlocker.Storage;

public sealed class AzureBlobPackageStorage : IPackageStorage
{
    private readonly BlobContainerClient _container;
    private readonly string _publicBaseUrl;

    public AzureBlobPackageStorage(BlobContainerClient container, string publicBaseUrl)
    {
        _container = container;
        _publicBaseUrl = publicBaseUrl.TrimEnd('/');
    }

    public async Task<StoredPackage> PutAsync(string key, Stream package, CancellationToken cancellationToken = default)
    {
        using var memory = new MemoryStream();
        await package.CopyToAsync(memory, cancellationToken);
        var bytes = memory.ToArray();
        memory.Position = 0;

        await _container.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
        await _container.GetBlobClient(key).UploadAsync(memory, overwrite: true, cancellationToken);

        return new StoredPackage
        {
            Key = key,
            Url = $"{_publicBaseUrl}/{Uri.EscapeDataString(key)}",
            Sha256 = Convert.ToHexString(SHA256.HashData(bytes)),
            SizeBytes = bytes.LongLength
        };
    }

    public async Task<Stream> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        var response = await _container.GetBlobClient(key).DownloadStreamingAsync(cancellationToken: cancellationToken);
        return response.Value.Content;
    }
}
