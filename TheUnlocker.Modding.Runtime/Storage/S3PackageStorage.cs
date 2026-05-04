using System.Security.Cryptography;
using Amazon.S3;
using Amazon.S3.Model;

namespace TheUnlocker.Storage;

public sealed class S3PackageStorage : IPackageStorage
{
    private readonly IAmazonS3 _client;
    private readonly string _bucket;
    private readonly string _publicBaseUrl;

    public S3PackageStorage(IAmazonS3 client, string bucket, string publicBaseUrl)
    {
        _client = client;
        _bucket = bucket;
        _publicBaseUrl = publicBaseUrl.TrimEnd('/');
    }

    public async Task<StoredPackage> PutAsync(string key, Stream package, CancellationToken cancellationToken = default)
    {
        using var memory = new MemoryStream();
        await package.CopyToAsync(memory, cancellationToken);
        var bytes = memory.ToArray();
        memory.Position = 0;

        await _client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _bucket,
            Key = key,
            InputStream = memory,
            AutoCloseStream = false
        }, cancellationToken);

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
        var response = await _client.GetObjectAsync(_bucket, key, cancellationToken);
        return response.ResponseStream;
    }
}
