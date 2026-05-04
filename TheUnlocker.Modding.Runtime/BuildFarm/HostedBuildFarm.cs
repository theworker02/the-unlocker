using TheUnlocker.Modding;
using TheUnlocker.Registry;

namespace TheUnlocker.BuildFarm;

public sealed class HostedBuildRequest
{
    public string PublisherId { get; init; } = "";
    public string ModId { get; init; } = "";
    public string Version { get; init; } = "";
    public string SourceUrl { get; init; } = "";
    public string CommitSha { get; init; } = "";
    public string BuildScript { get; init; } = "dotnet build";
    public bool SignArtifacts { get; init; } = true;
    public bool GenerateSbom { get; init; } = true;
    public bool ScanBeforePublish { get; init; } = true;
}

public sealed class HostedBuildResult
{
    public string BuildId { get; init; } = Guid.NewGuid().ToString("N");
    public string ModId { get; init; } = "";
    public string Version { get; init; } = "";
    public string Status { get; init; } = "Queued";
    public string ArtifactUrl { get; init; } = "";
    public string SignatureUrl { get; init; } = "";
    public string SbomUrl { get; init; } = "";
    public PackageProvenance Provenance { get; init; } = new();
    public string[] Steps { get; init; } = [];
}

public sealed class HostedBuildFarmService
{
    public HostedBuildResult QueueBuild(HostedBuildRequest request)
    {
        var buildId = Guid.NewGuid().ToString("N");
        var artifactName = $"{request.ModId}-{request.Version}.zip";

        return new HostedBuildResult
        {
            BuildId = buildId,
            ModId = request.ModId,
            Version = request.Version,
            Status = "Queued",
            ArtifactUrl = $"buildfarm://artifacts/{buildId}/{artifactName}",
            SignatureUrl = request.SignArtifacts ? $"buildfarm://artifacts/{buildId}/{artifactName}.signature.json" : "",
            SbomUrl = request.GenerateSbom ? $"buildfarm://artifacts/{buildId}/sbom.json" : "",
            Provenance = new PackageProvenance
            {
                PackageId = request.ModId,
                UploadedBy = request.PublisherId,
                CommitSha = request.CommitSha,
                CiRunUrl = $"buildfarm://jobs/{buildId}",
                Timestamp = DateTimeOffset.UtcNow
            },
            Steps =
            [
                "restore source",
                "run reproducible build",
                request.GenerateSbom ? "generate SBOM" : "skip SBOM",
                request.ScanBeforePublish ? "scan package" : "skip scan",
                request.SignArtifacts ? "sign artifact" : "skip signing",
                "publish artifact after policy approval"
            ]
        };
    }
}
