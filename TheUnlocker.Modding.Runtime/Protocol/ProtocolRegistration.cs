namespace TheUnlocker.Protocol;

public sealed class ProtocolRegistration
{
    public const string Protocol = "theunlocker";

    public string CreateRegistryFileContent(string executablePath)
    {
        var escaped = executablePath.Replace("\\", "\\\\", StringComparison.Ordinal);
        return $"""
Windows Registry Editor Version 5.00

[HKEY_CURRENT_USER\Software\Classes\{Protocol}]
@="URL:TheUnlocker Install Protocol"
"URL Protocol"=""

[HKEY_CURRENT_USER\Software\Classes\{Protocol}\shell\open\command]
@="\"{escaped}\" \"%1\""
""";
    }

    public string? TryParseInstallUri(string uri)
    {
        if (!Uri.TryCreate(uri, UriKind.Absolute, out var parsed) || !parsed.Scheme.Equals(Protocol, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var path = parsed.AbsolutePath.Trim('/');
        if (!parsed.Host.Equals("install", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        return Uri.UnescapeDataString(path);
    }

    public string? TryParseInstallPackUri(string uri)
    {
        if (!Uri.TryCreate(uri, UriKind.Absolute, out var parsed) || !parsed.Scheme.Equals(Protocol, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var path = parsed.AbsolutePath.Trim('/');
        if (!parsed.Host.Equals("install-pack", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        return Uri.UnescapeDataString(path);
    }
}
