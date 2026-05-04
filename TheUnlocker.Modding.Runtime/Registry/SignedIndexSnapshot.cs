using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using TheUnlocker.Modding;

namespace TheUnlocker.Registry;

public sealed class SignedIndexSnapshot
{
    public ModRepositoryIndex Index { get; init; } = new();
    public string SignatureBase64 { get; init; } = "";
    public string PublicKeyPem { get; init; } = "";
    public DateTimeOffset SignedAt { get; init; } = DateTimeOffset.UtcNow;
}

public sealed class SignedIndexService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false, PropertyNameCaseInsensitive = true };

    public SignedIndexSnapshot Sign(ModRepositoryIndex index, string privateKeyPem, string publicKeyPem)
    {
        using var rsa = RSA.Create();
        rsa.ImportFromPem(privateKeyPem);
        var payload = JsonSerializer.Serialize(index, JsonOptions);
        var signature = rsa.SignData(Encoding.UTF8.GetBytes(payload), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return new SignedIndexSnapshot
        {
            Index = index,
            PublicKeyPem = publicKeyPem,
            SignatureBase64 = Convert.ToBase64String(signature)
        };
    }

    public bool Verify(SignedIndexSnapshot snapshot)
    {
        using var rsa = RSA.Create();
        rsa.ImportFromPem(snapshot.PublicKeyPem);
        var payload = JsonSerializer.Serialize(snapshot.Index, JsonOptions);
        return rsa.VerifyData(
            Encoding.UTF8.GetBytes(payload),
            Convert.FromBase64String(snapshot.SignatureBase64),
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
    }
}
