using System.Security.Cryptography;
using System.Text.Json;
using TheUnlocker.Modding;

namespace TheUnlocker.Workspaces;

public sealed class ModpackLockfileResolver
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly HttpClient _http = new();

    public async Task<IReadOnlyCollection<string>> InstallAsync(string lockfileUrlOrPath, ModInstaller installer, CancellationToken cancellationToken = default)
    {
        var json = Uri.TryCreate(lockfileUrlOrPath, UriKind.Absolute, out var uri) && uri.Scheme.StartsWith("http", StringComparison.OrdinalIgnoreCase)
            ? await _http.GetStringAsync(uri, cancellationToken)
            : await File.ReadAllTextAsync(lockfileUrlOrPath, cancellationToken);

        var lockfile = JsonSerializer.Deserialize<UnlockerLockFile>(json, JsonOptions) ?? new UnlockerLockFile();
        var installed = new List<string>();
        foreach (var mod in lockfile.Mods)
        {
            var package = await DownloadAsync(mod.Source, cancellationToken);
            var actualHash = ComputeSha256(package);
            if (!string.IsNullOrWhiteSpace(mod.Sha256) && !actualHash.Equals(mod.Sha256, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Package hash mismatch for {mod.Id}.");
            }

            installed.Add(installer.Install(package));
        }

        return installed;
    }

    private async Task<string> DownloadAsync(string source, CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(source, UriKind.Absolute, out var uri) || !uri.Scheme.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            return source;
        }

        var target = Path.Combine(Path.GetTempPath(), $"theunlocker-pack-{Guid.NewGuid():N}.zip");
        await using var stream = await _http.GetStreamAsync(uri, cancellationToken);
        await using var file = File.Create(target);
        await stream.CopyToAsync(file, cancellationToken);
        return target;
    }

    private static string ComputeSha256(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream));
    }
}
