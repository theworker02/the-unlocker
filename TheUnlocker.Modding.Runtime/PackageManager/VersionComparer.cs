namespace TheUnlocker.PackageManager;

public sealed class VersionComparer : IComparer<string>
{
    public static VersionComparer Instance { get; } = new();

    public int Compare(string? left, string? right)
    {
        return CompareVersions(left ?? "", right ?? "");
    }

    public static bool Satisfies(string versionText, string range)
    {
        if (string.IsNullOrWhiteSpace(range) || !Version.TryParse(versionText, out var version))
        {
            return true;
        }

        foreach (var part in range.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (part.StartsWith(">=", StringComparison.Ordinal) && Version.TryParse(part[2..], out var min) && version < min)
            {
                return false;
            }

            if (part.StartsWith("<=", StringComparison.Ordinal) && Version.TryParse(part[2..], out var maxInclusive) && version > maxInclusive)
            {
                return false;
            }

            if (part.StartsWith("<", StringComparison.Ordinal) && Version.TryParse(part[1..], out var max) && version >= max)
            {
                return false;
            }

            if (part.StartsWith(">", StringComparison.Ordinal) && Version.TryParse(part[1..], out var minExclusive) && version <= minExclusive)
            {
                return false;
            }

            if (!part.Any(c => c is '<' or '>') && Version.TryParse(part, out var exact) && version != exact)
            {
                return false;
            }
        }

        return true;
    }

    public static int CompareVersions(string left, string right)
    {
        Version.TryParse(left, out var leftVersion);
        Version.TryParse(right, out var rightVersion);
        return Comparer<Version>.Default.Compare(leftVersion ?? new Version(0, 0, 0), rightVersion ?? new Version(0, 0, 0));
    }
}
