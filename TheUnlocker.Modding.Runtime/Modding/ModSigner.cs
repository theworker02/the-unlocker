using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace TheUnlocker.Modding;

public static class ModSigner
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };

    public static string GenerateKeys(string outputDirectory, string publisherId)
    {
        Directory.CreateDirectory(outputDirectory);
        using var rsa = RSA.Create(3072);
        File.WriteAllText(Path.Combine(outputDirectory, $"{publisherId}.private.pem"), rsa.ExportRSAPrivateKeyPem());
        File.WriteAllText(Path.Combine(outputDirectory, $"{publisherId}.public.pem"), rsa.ExportRSAPublicKeyPem());
        return Path.Combine(outputDirectory, $"{publisherId}.public.pem");
    }

    public static void SignManifest(string modDirectory, string privateKeyPath)
    {
        var manifestPath = Path.Combine(modDirectory, "mod.json");
        var manifest = JsonSerializer.Deserialize<ModManifest>(File.ReadAllText(manifestPath), JsonOptions)
            ?? throw new InvalidOperationException("mod.json could not be read.");
        var dllPath = Path.Combine(modDirectory, manifest.EntryDll);
        var sha256 = ComputeSha256(dllPath);
        var payload = $"{manifest.Id}|{manifest.Version}|{sha256}";

        using var rsa = RSA.Create();
        rsa.ImportFromPem(File.ReadAllText(privateKeyPath));
        var signature = Convert.ToBase64String(rsa.SignData(Encoding.UTF8.GetBytes(payload), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));

        manifest = CopyWithSignature(manifest, sha256, signature);
        File.WriteAllText(manifestPath, JsonSerializer.Serialize(manifest, JsonOptions));
    }

    public static bool VerifyManifest(string modDirectory, string publicKeyPath)
    {
        var manifestPath = Path.Combine(modDirectory, "mod.json");
        var manifest = JsonSerializer.Deserialize<ModManifest>(File.ReadAllText(manifestPath), JsonOptions)
            ?? throw new InvalidOperationException("mod.json could not be read.");
        if (manifest.Signature is null)
        {
            return false;
        }
        if (string.IsNullOrWhiteSpace(manifest.Signature.RsaSha256))
        {
            return false;
        }

        var dllPath = Path.Combine(modDirectory, manifest.EntryDll);
        if (!File.Exists(dllPath))
        {
            return false;
        }

        var sha256 = ComputeSha256(dllPath);
        if (!sha256.Equals(manifest.Signature.Sha256, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var payload = $"{manifest.Id}|{manifest.Version}|{sha256}";
        using var rsa = RSA.Create();
        rsa.ImportFromPem(File.ReadAllText(publicKeyPath));
        return rsa.VerifyData(
            Encoding.UTF8.GetBytes(payload),
            Convert.FromBase64String(manifest.Signature.RsaSha256),
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
    }

    public static string RotateKeys(string outputDirectory, string publisherId, string previousPublicKeyPath)
    {
        var newPublicKey = GenerateKeys(outputDirectory, $"{publisherId}-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}");
        File.WriteAllText(Path.Combine(outputDirectory, $"{publisherId}.rotation.json"), JsonSerializer.Serialize(new
        {
            publisherId,
            previousPublicKey = Path.GetFileName(previousPublicKeyPath),
            newPublicKey = Path.GetFileName(newPublicKey),
            rotatedAt = DateTimeOffset.UtcNow
        }, JsonOptions));
        return newPublicKey;
    }

    public static string RevokeKey(string outputDirectory, string publisherId, string keyId, string reason)
    {
        Directory.CreateDirectory(outputDirectory);
        var path = Path.Combine(outputDirectory, $"{publisherId}.{keyId}.revoked.json");
        File.WriteAllText(path, JsonSerializer.Serialize(new
        {
            publisherId,
            keyId,
            reason,
            revokedAt = DateTimeOffset.UtcNow
        }, JsonOptions));
        return path;
    }

    private static ModManifest CopyWithSignature(ModManifest manifest, string sha256, string signature)
    {
        return new ModManifest
        {
            Id = manifest.Id,
            Name = manifest.Name,
            Version = manifest.Version,
            Author = manifest.Author,
            Description = manifest.Description,
            EntryDll = manifest.EntryDll,
            MinimumAppVersion = manifest.MinimumAppVersion,
            MinimumFrameworkVersion = manifest.MinimumFrameworkVersion,
            SdkVersion = manifest.SdkVersion,
            DependsOn = manifest.DependsOn,
            Dependencies = manifest.Dependencies,
            PeerDependencies = manifest.PeerDependencies,
            Permissions = manifest.Permissions,
            Targets = manifest.Targets,
            Settings = manifest.Settings,
            PublisherId = manifest.PublisherId,
            TrustLevel = manifest.TrustLevel,
            IsolationMode = manifest.IsolationMode,
            EventSchemas = manifest.EventSchemas,
            CommandScopes = manifest.CommandScopes,
            Signature = new ModSignature { Sha256 = sha256, RsaSha256 = signature }
        };
    }

    private static string ComputeSha256(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream));
    }
}
