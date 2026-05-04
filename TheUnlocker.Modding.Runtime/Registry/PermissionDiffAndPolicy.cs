using TheUnlocker.Configuration;
using TheUnlocker.Modding;

namespace TheUnlocker.Registry;

public sealed class PermissionDiff
{
    public string ModId { get; init; } = "";
    public string[] Added { get; init; } = [];
    public string[] Removed { get; init; } = [];
    public bool RequiresReapproval => Added.Length > 0;
}

public sealed class PermissionDiffService
{
    public PermissionDiff Compare(ModManifest currentManifest, ModManifest updateManifest)
    {
        var current = currentManifest.Permissions.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var update = updateManifest.Permissions.ToHashSet(StringComparer.OrdinalIgnoreCase);

        return new PermissionDiff
        {
            ModId = updateManifest.Id,
            Added = update.Except(current, StringComparer.OrdinalIgnoreCase).Order().ToArray(),
            Removed = current.Except(update, StringComparer.OrdinalIgnoreCase).Order().ToArray()
        };
    }
}

public sealed class SignatureEnforcementService
{
    public SignaturePolicyDecision Evaluate(ModManifest manifest, ModSignatureStatus signatureStatus, ModPolicy policy)
    {
        if (policy.BlockedMods.Contains(manifest.Id))
        {
            return new SignaturePolicyDecision(false, "The mod is blocked by policy.");
        }

        if (!policy.AllowUnsignedMods && signatureStatus != ModSignatureStatus.Verified)
        {
            return new SignaturePolicyDecision(false, "Unsigned mods are blocked by policy.");
        }

        if (policy.AllowedPublishers.Count > 0 && !policy.AllowedPublishers.Contains(manifest.Author))
        {
            return new SignaturePolicyDecision(false, "The publisher is not on the allowlist.");
        }

        return new SignaturePolicyDecision(true, "Signature policy accepted.");
    }
}

public sealed record SignaturePolicyDecision(bool Allowed, string Reason);
