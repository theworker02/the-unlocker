namespace TheUnlocker.Registry;

public sealed class PackageProvenance
{
    public string PackageId { get; init; } = "";
    public string UploadedBy { get; init; } = "";
    public string SignedBy { get; init; } = "";
    public string CommitSha { get; init; } = "";
    public string CiRunUrl { get; init; } = "";
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    public string Sha256 { get; init; } = "";
    public string SourceRepository { get; init; } = "";
}

public sealed class ReproducibleBuildRequest
{
    public string ModId { get; init; } = "";
    public string Version { get; init; } = "";
    public string RepositoryUrl { get; init; } = "";
    public string CommitSha { get; init; } = "";
    public string BuildCommand { get; init; } = "dotnet build";
    public string ExpectedSha256 { get; init; } = "";
}

public sealed class CertificationBadge
{
    public string Name { get; init; } = "";
    public string Issuer { get; init; } = "";
    public DateTimeOffset IssuedAt { get; init; } = DateTimeOffset.UtcNow;
    public string EvidenceUrl { get; init; } = "";
}

public sealed class MarketplaceSearchQuery
{
    public string? Text { get; init; }
    public string? GameId { get; init; }
    public string? Version { get; init; }
    public string? Permission { get; init; }
    public string? TrustLevel { get; init; }
    public string? Dependency { get; init; }
    public string? Tag { get; init; }
    public int? MinimumRating { get; init; }
    public DateTimeOffset? UpdatedSince { get; init; }
}
