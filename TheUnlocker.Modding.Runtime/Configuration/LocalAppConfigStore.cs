using System.IO;
using System.Text.Json;

namespace TheUnlocker.Configuration;

public sealed class LocalAppConfigStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private readonly string _configPath;

    public LocalAppConfigStore(string configPath)
    {
        _configPath = configPath;
    }

    public LocalAppConfig Load()
    {
        EnsureExists();

        try
        {
            var json = File.ReadAllText(_configPath);
            return JsonSerializer.Deserialize<LocalAppConfig>(json, JsonOptions) ?? new LocalAppConfig();
        }
        catch
        {
            return new LocalAppConfig();
        }
    }

    public void Save(LocalAppConfig config)
    {
        var directory = Path.GetDirectoryName(_configPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(_configPath, JsonSerializer.Serialize(config, JsonOptions));
    }

    public void Update(Action<LocalAppConfig> update)
    {
        var config = Load();
        update(config);
        Save(config);
    }

    private void EnsureExists()
    {
        var directory = Path.GetDirectoryName(_configPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        if (!File.Exists(_configPath))
        {
            Save(new LocalAppConfig());
        }
    }
}
