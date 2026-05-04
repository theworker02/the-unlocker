using TheUnlocker.Modding;

namespace TheUnlocker.Registry;

public sealed class PackageReputationScore
{
    public string ModId { get; init; } = "";
    public int Score { get; init; }
    public string[] Factors { get; init; } = [];
}

public sealed class PackageReputationService
{
    public PackageReputationScore Score(
        ModManifest manifest,
        ModSignatureStatus signatureStatus,
        bool scanClean,
        int reportCount,
        int installCount,
        int updateCount)
    {
        var score = 50;
        var factors = new List<string>();

        if (signatureStatus == ModSignatureStatus.Verified)
        {
            score += 20;
            factors.Add("Verified signature");
        }

        if (manifest.TrustLevel is ModTrustLevel.Official or ModTrustLevel.TrustedPublisher)
        {
            score += 15;
            factors.Add($"Trust level: {manifest.TrustLevel}");
        }

        if (scanClean)
        {
            score += 10;
            factors.Add("Clean package scan");
        }

        score += Math.Min(10, installCount / 100);
        score += Math.Min(5, updateCount);
        score -= Math.Min(40, reportCount * 5);

        return new PackageReputationScore
        {
            ModId = manifest.Id,
            Score = Math.Clamp(score, 0, 100),
            Factors = factors.ToArray()
        };
    }
}
