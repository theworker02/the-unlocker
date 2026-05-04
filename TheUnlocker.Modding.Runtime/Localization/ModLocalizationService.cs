using System.Text.Json;

namespace TheUnlocker.Localization;

public sealed class LocalizedStringSet
{
    public string Locale { get; init; } = "en-US";
    public Dictionary<string, string> Strings { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class ModLocalizationService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public LocalizedStringSet Load(string modDirectory, string locale)
    {
        var path = Path.Combine(modDirectory, "locale", $"{locale}.json");
        if (!File.Exists(path))
        {
            path = Path.Combine(modDirectory, "locale", "en-US.json");
        }

        if (!File.Exists(path))
        {
            return new LocalizedStringSet { Locale = locale };
        }

        return JsonSerializer.Deserialize<LocalizedStringSet>(File.ReadAllText(path), JsonOptions) ?? new LocalizedStringSet { Locale = locale };
    }
}
