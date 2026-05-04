using System.Text.Json;
using TheUnlocker.Modding;

namespace TheUnlocker.Desktop;

public sealed class OfflineRegistryCache
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };
    private readonly string _path;

    public OfflineRegistryCache(string path)
    {
        _path = path;
    }

    public void Save(ModRepositoryIndex index)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path) ?? ".");
        File.WriteAllText(_path, JsonSerializer.Serialize(index, JsonOptions));
    }

    public ModRepositoryIndex Load()
    {
        if (!File.Exists(_path))
        {
            return new ModRepositoryIndex();
        }

        return JsonSerializer.Deserialize<ModRepositoryIndex>(File.ReadAllText(_path), JsonOptions) ?? new ModRepositoryIndex();
    }
}
