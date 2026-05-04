using System.IO;
using System.IO.Compression;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using TheUnlocker.Registry;

namespace TheUnlocker.Modding;

public static class ModPackager
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public static string Package(string modProjectDirectory, string outputDirectory)
    {
        var manifestPath = Path.Combine(modProjectDirectory, "mod.json");
        if (!File.Exists(manifestPath))
        {
            throw new InvalidOperationException("mod.json was not found in the mod project directory.");
        }

        BuildModProject(modProjectDirectory);

        var manifest = JsonSerializer.Deserialize<ModManifest>(File.ReadAllText(manifestPath), JsonOptions)
            ?? throw new InvalidOperationException("mod.json could not be read.");

        var buildOutput = Path.Combine(modProjectDirectory, "bin", "Debug", "net8.0-windows");
        var entryDllPath = Path.Combine(buildOutput, manifest.EntryDll);
        if (!File.Exists(entryDllPath))
        {
            throw new InvalidOperationException($"Built entry DLL was not found: {entryDllPath}");
        }

        manifest = WithSignature(manifest, ComputeSha256(entryDllPath));

        var staging = Path.Combine(Path.GetTempPath(), $"mod-package-{Guid.NewGuid():N}");
        Directory.CreateDirectory(staging);
        Directory.CreateDirectory(outputDirectory);

        try
        {
            foreach (var file in Directory.EnumerateFiles(buildOutput))
            {
                var extension = Path.GetExtension(file);
                if (extension.Equals(".dll", StringComparison.OrdinalIgnoreCase)
                    || extension.Equals(".json", StringComparison.OrdinalIgnoreCase)
                    || extension.Equals(".pdb", StringComparison.OrdinalIgnoreCase))
                {
                    File.Copy(file, Path.Combine(staging, Path.GetFileName(file)), overwrite: true);
                }
            }

            File.WriteAllText(Path.Combine(staging, "mod.json"), JsonSerializer.Serialize(manifest, JsonOptions));
            File.WriteAllText(
                Path.Combine(staging, "sbom.json"),
                JsonSerializer.Serialize(new SbomGenerator().Generate(manifest.Id, manifest.Version, staging), JsonOptions));

            var packageName = $"{manifest.Id}-{manifest.Version}.zip";
            var packagePath = Path.Combine(outputDirectory, packageName);
            if (File.Exists(packagePath))
            {
                File.Delete(packagePath);
            }

            ZipFile.CreateFromDirectory(staging, packagePath);
            return packagePath;
        }
        finally
        {
            if (Directory.Exists(staging))
            {
                Directory.Delete(staging, recursive: true);
            }
        }
    }

    private static ModManifest WithSignature(ModManifest manifest, string sha256)
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
            Permissions = manifest.Permissions,
            Targets = manifest.Targets,
            Settings = manifest.Settings,
            PublisherId = manifest.PublisherId,
            Signature = new ModSignature { Sha256 = sha256 }
        };
    }

    private static string ComputeSha256(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream));
    }

    private static void BuildModProject(string modProjectDirectory)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "build",
            WorkingDirectory = modProjectDirectory,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false
        };

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Could not start dotnet build.");
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd());
        }
    }
}
