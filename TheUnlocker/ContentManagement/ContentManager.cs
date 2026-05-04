using System.IO;
using System.Text.Json;
using TheUnlocker.Configuration;

namespace TheUnlocker.ContentManagement;

public sealed class ContentManager : IContentManager
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private readonly string _modulesDirectory;
    private readonly LocalAppConfigStore _configStore;
    private readonly Dictionary<string, ModuleInfo> _modules = new(StringComparer.OrdinalIgnoreCase);

    private LocalAppConfig _config = new();

    public ContentManager(string modulesDirectory, string configPath)
    {
        _modulesDirectory = modulesDirectory;
        _configStore = new LocalAppConfigStore(configPath);
    }

    public IReadOnlyCollection<ModuleInfo> Modules => _modules.Values;

    public void Refresh()
    {
        _modules.Clear();

        EnsureLocalFilesExist();
        LoadConfig();
        DiscoverModules();
        ApplyConfiguredState();
    }

    public bool IsModuleAvailable(string moduleId)
    {
        return _modules.TryGetValue(moduleId, out var module)
            && module.Status is ModuleStatus.Available or ModuleStatus.Active;
    }

    public bool IsModuleEnabled(string moduleId)
    {
        return _modules.TryGetValue(moduleId, out var module)
            && module.Status == ModuleStatus.Active;
    }

    public ModuleInfo? GetModule(string moduleId)
    {
        return _modules.GetValueOrDefault(moduleId);
    }

    public void SetModuleEnabled(string moduleId, bool enabled)
    {
        _configStore.Update(config =>
        {
            if (enabled)
            {
                config.EnabledModules.Add(moduleId);
            }
            else
            {
                config.EnabledModules.Remove(moduleId);
            }
        });

        Refresh();
    }

    private void EnsureLocalFilesExist()
    {
        Directory.CreateDirectory(_modulesDirectory);
        _configStore.Save(_configStore.Load());
    }

    private void LoadConfig()
    {
        try
        {
            _config = _configStore.Load();
        }
        catch (Exception ex)
        {
            _config = new LocalAppConfig();

            _modules["content-config"] = new ModuleInfo
            {
                Id = "content-config",
                Name = "Content configuration",
                Path = "content-config.json",
                Status = ModuleStatus.Error,
                ErrorMessage = $"Configuration could not be read. Optional modules were disabled. {ex.Message}"
            };
        }
    }

    private void DiscoverModules()
    {
        foreach (var manifestPath in Directory.EnumerateFiles(_modulesDirectory, "*.manifest.json", SearchOption.AllDirectories))
        {
            TryLoadManifest(manifestPath);
        }

        foreach (var manifestPath in Directory.EnumerateFiles(_modulesDirectory, "*.manifest", SearchOption.AllDirectories))
        {
            TryLoadManifest(manifestPath);
        }

        foreach (var dllPath in Directory.EnumerateFiles(_modulesDirectory, "*.dll", SearchOption.AllDirectories))
        {
            TryRegisterDllOnlyModule(dllPath);
        }
    }

    private void TryLoadManifest(string manifestPath)
    {
        try
        {
            var json = File.ReadAllText(manifestPath);
            var manifest = JsonSerializer.Deserialize<ModuleManifest>(json, JsonOptions);

            if (manifest is null || string.IsNullOrWhiteSpace(manifest.Id))
            {
                AddErrorModule(
                    Path.GetFileNameWithoutExtension(manifestPath),
                    manifestPath,
                    "Manifest is missing a required module id.");
                return;
            }

            var moduleDirectory = Path.GetDirectoryName(manifestPath) ?? _modulesDirectory;
            var module = new ModuleInfo
            {
                Id = manifest.Id,
                Name = string.IsNullOrWhiteSpace(manifest.Name) ? manifest.Id : manifest.Name,
                Version = manifest.Version,
                Path = moduleDirectory,
                Status = ModuleStatus.Available
            };

            if (!string.IsNullOrWhiteSpace(manifest.EntryDll))
            {
                var entryDllPath = System.IO.Path.Combine(moduleDirectory, manifest.EntryDll);
                if (!File.Exists(entryDllPath))
                {
                    module.Status = ModuleStatus.Error;
                    module.ErrorMessage = $"Configured entry DLL is missing: {manifest.EntryDll}";
                }
            }

            _modules[module.Id] = module;
        }
        catch (Exception ex)
        {
            AddErrorModule(
                Path.GetFileNameWithoutExtension(manifestPath),
                manifestPath,
                $"Manifest could not be loaded. {ex.Message}");
        }
    }

    private void TryRegisterDllOnlyModule(string dllPath)
    {
        var id = Path.GetFileNameWithoutExtension(dllPath);

        if (_modules.ContainsKey(id))
        {
            return;
        }

        _modules[id] = new ModuleInfo
        {
            Id = id,
            Name = id,
            Path = dllPath,
            Status = ModuleStatus.Available
        };
    }

    private void ApplyConfiguredState()
    {
        foreach (var enabledModuleId in _config.EnabledModules)
        {
            if (!_modules.TryGetValue(enabledModuleId, out var module))
            {
                _modules[enabledModuleId] = new ModuleInfo
                {
                    Id = enabledModuleId,
                    Name = enabledModuleId,
                    Path = _modulesDirectory,
                    Status = ModuleStatus.Missing,
                    ErrorMessage = "Enabled in local configuration, but no matching manifest or DLL was found."
                };

                continue;
            }

            if (module.Status == ModuleStatus.Available)
            {
                module.Status = ModuleStatus.Active;
            }
        }
    }

    private void AddErrorModule(string id, string path, string message)
    {
        _modules[id] = new ModuleInfo
        {
            Id = id,
            Name = id,
            Path = path,
            Status = ModuleStatus.Error,
            ErrorMessage = message
        };
    }
}
