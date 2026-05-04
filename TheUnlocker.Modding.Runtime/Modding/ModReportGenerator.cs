using System.IO;
using System.Text;
using System.Text.Json;

namespace TheUnlocker.Modding;

public static class ModReportGenerator
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public static string GenerateCompatibilityReport(
        string outputDirectory,
        Version appVersion,
        IEnumerable<string> manifestPaths)
    {
        Directory.CreateDirectory(outputDirectory);
        var path = Path.Combine(outputDirectory, $"compatibility-{DateTimeOffset.Now:yyyyMMdd-HHmmss}.md");
        var builder = new StringBuilder();
        builder.AppendLine("# Compatibility Report");
        builder.AppendLine();
        builder.AppendLine($"App version: `{appVersion}`");
        builder.AppendLine();

        foreach (var manifestPath in manifestPaths)
        {
            var manifest = JsonSerializer.Deserialize<ModManifest>(File.ReadAllText(manifestPath), JsonOptions);
            if (manifest is null)
            {
                continue;
            }

            var compatible = IsCompatible(manifest, appVersion);
            builder.AppendLine($"## {manifest.Name} (`{manifest.Id}`)");
            builder.AppendLine();
            builder.AppendLine($"- Version: `{manifest.Version}`");
            builder.AppendLine($"- SDK: `{manifest.SdkVersion}`");
            builder.AppendLine($"- Compatible: `{compatible}`");
            builder.AppendLine();
        }

        File.WriteAllText(path, builder.ToString());
        return path;
    }

    public static string GenerateModDocumentation(string manifestPath, string outputDirectory)
    {
        Directory.CreateDirectory(outputDirectory);
        var manifest = JsonSerializer.Deserialize<ModManifest>(File.ReadAllText(manifestPath), JsonOptions)
            ?? throw new InvalidOperationException("Manifest could not be read.");

        var path = Path.Combine(outputDirectory, $"{manifest.Id}-docs.md");
        var builder = new StringBuilder();
        builder.AppendLine($"# {manifest.Name}");
        builder.AppendLine();
        builder.AppendLine(manifest.Description);
        builder.AppendLine();
        builder.AppendLine($"- ID: `{manifest.Id}`");
        builder.AppendLine($"- Version: `{manifest.Version}`");
        builder.AppendLine($"- Author: `{manifest.Author}`");
        builder.AppendLine($"- SDK: `{manifest.SdkVersion}`");
        builder.AppendLine($"- Permissions: `{string.Join(", ", manifest.Permissions)}`");
        builder.AppendLine($"- Targets: `{string.Join(", ", manifest.Targets)}`");
        builder.AppendLine($"- Event schemas: `{string.Join(", ", manifest.EventSchemas)}`");
        builder.AppendLine();
        builder.AppendLine("## Settings");
        foreach (var setting in manifest.Settings)
        {
            builder.AppendLine($"- `{setting.Key}` ({setting.Value.Type}): {setting.Value.Label} Default: `{setting.Value.DefaultValue}`");
        }
        builder.AppendLine();
        builder.AppendLine("## Commands");
        foreach (var command in manifest.CommandScopes)
        {
            builder.AppendLine($"- `{command.Key}` scopes: `{string.Join(", ", command.Value)}`");
        }

        File.WriteAllText(path, builder.ToString());
        return path;
    }

    private static bool IsCompatible(ModManifest manifest, Version appVersion)
    {
        return (!Version.TryParse(manifest.MinimumAppVersion, out var minimumApp) || appVersion >= minimumApp)
            && (!Version.TryParse(manifest.SdkVersion, out var sdkVersion) || sdkVersion.Major == 1);
    }
}
