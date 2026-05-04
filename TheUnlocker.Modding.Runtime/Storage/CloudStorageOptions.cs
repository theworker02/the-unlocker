namespace TheUnlocker.Storage;

public sealed class CloudStorageOptions
{
    public string Provider { get; init; } = "local";
    public string Endpoint { get; init; } = "";
    public string BucketOrContainer { get; init; } = "";
    public string AccessKeyEnvironmentVariable { get; init; } = "";
    public string SecretKeyEnvironmentVariable { get; init; } = "";
}

public sealed class ObjectStorageDescriptor
{
    public string Provider { get; init; } = "";
    public string UploadUrl { get; init; } = "";
    public string Notes { get; init; } = "";
}
