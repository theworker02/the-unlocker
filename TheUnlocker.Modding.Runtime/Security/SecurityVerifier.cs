using System.Security.Cryptography;
using TheUnlocker.Modding;

namespace TheUnlocker.Security;

public sealed class SecurityVerifier
{
    public bool VerifyHash(string path, string expectedSha256)
    {
        if (string.IsNullOrWhiteSpace(expectedSha256) || !File.Exists(path))
        {
            return false;
        }

        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).Equals(expectedSha256, StringComparison.OrdinalIgnoreCase);
    }

    public bool HasPermission(ModManifest manifest, string permission)
    {
        return manifest.Permissions.Contains(permission, StringComparer.OrdinalIgnoreCase);
    }
}
