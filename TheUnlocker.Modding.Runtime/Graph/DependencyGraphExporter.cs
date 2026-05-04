using System.Text;
using TheUnlocker.Modding;

namespace TheUnlocker.Graph;

public sealed class DependencyGraphExporter
{
    public string ToMermaid(IEnumerable<ModManifest> manifests)
    {
        var builder = new StringBuilder("graph TD").AppendLine();
        foreach (var manifest in manifests.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase))
        {
            builder.AppendLine($"  {Node(manifest.Id)}[\"{manifest.Name} ({manifest.Id})\"]");
            foreach (var dependency in manifest.DependsOn)
            {
                builder.AppendLine($"  {Node(dependency)} --> {Node(manifest.Id)}");
            }

            foreach (var dependency in manifest.Dependencies)
            {
                var label = string.IsNullOrWhiteSpace(dependency.VersionRange) ? dependency.Id : $"{dependency.Id} {dependency.VersionRange}";
                builder.AppendLine($"  {Node(dependency.Id)} -->|\"{label}\"| {Node(manifest.Id)}");
            }
        }

        return builder.ToString();
    }

    private static string Node(string value)
    {
        var clean = new string(value.Select(ch => char.IsLetterOrDigit(ch) ? ch : '_').ToArray());
        return string.IsNullOrWhiteSpace(clean) ? "unknown" : clean;
    }
}
