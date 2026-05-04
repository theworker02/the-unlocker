using TheUnlocker.Configuration;
using TheUnlocker.Modding;

namespace TheUnlocker.Desktop;

public sealed class TrustPolicyEditorService
{
    private readonly LocalAppConfigStore _store;

    public TrustPolicyEditorService(string configPath)
    {
        _store = new LocalAppConfigStore(configPath);
    }

    public LocalAppConfig Load() => _store.Load();

    public void Save(LocalAppConfig config) => _store.Save(config);

    public void ToggleUnsignedMods()
    {
        _store.Update(config => config.Policy.AllowUnsignedMods = !config.Policy.AllowUnsignedMods);
    }

    public void AddPrivateRegistry(string url)
    {
        if (!string.IsNullOrWhiteSpace(url))
        {
            _store.Update(config => config.Policy.PrivateRegistryUrls.Add(url));
        }
    }

    public void ResetPolicies()
    {
        _store.Update(config =>
        {
            config.Policy.AllowUnsignedMods = true;
            config.Policy.AllowedPublishers.Clear();
            config.Policy.BlockedMods.Clear();
            config.Policy.PrivateRegistryUrls.Clear();
            config.Policy.PermissionDefaults.Clear();
        });
    }
}

public sealed class PermissionSimulationReport
{
    public string ModId { get; init; } = "";
    public string[] FileAccess { get; init; } = [];
    public string[] NetworkAccess { get; init; } = [];
    public string[] UiExtensions { get; init; } = [];
    public string[] GameSystems { get; init; } = [];

    public override string ToString()
    {
        return $"{ModId}: files [{string.Join(", ", FileAccess.DefaultIfEmpty("none"))}], network [{string.Join(", ", NetworkAccess.DefaultIfEmpty("none"))}], UI [{string.Join(", ", UiExtensions.DefaultIfEmpty("none"))}], systems [{string.Join(", ", GameSystems.DefaultIfEmpty("none"))}]";
    }
}

public sealed class PermissionSimulator
{
    public PermissionSimulationReport Simulate(ModManifest manifest)
    {
        return new PermissionSimulationReport
        {
            ModId = manifest.Id,
            FileAccess = manifest.Permissions.Where(p => p.Contains("File", StringComparison.OrdinalIgnoreCase) || p.Contains("Asset", StringComparison.OrdinalIgnoreCase)).ToArray(),
            NetworkAccess = manifest.Permissions.Where(p => p.Contains("Network", StringComparison.OrdinalIgnoreCase)).ToArray(),
            UiExtensions = manifest.Permissions.Where(p => p.Contains("Menu", StringComparison.OrdinalIgnoreCase)
                || p.Contains("Command", StringComparison.OrdinalIgnoreCase)
                || p.Contains("Theme", StringComparison.OrdinalIgnoreCase)
                || p.Contains("Panel", StringComparison.OrdinalIgnoreCase)).ToArray(),
            GameSystems = manifest.Targets
        };
    }
}

public sealed class LocalModDevelopmentService
{
    public string LinkProject(string projectDirectory, string modsDirectory)
    {
        if (!Directory.Exists(projectDirectory))
        {
            throw new DirectoryNotFoundException(projectDirectory);
        }

        Directory.CreateDirectory(modsDirectory);
        var target = Path.Combine(modsDirectory, Path.GetFileName(projectDirectory));
        if (Directory.Exists(target))
        {
            return target;
        }

        try
        {
            Directory.CreateSymbolicLink(target, projectDirectory);
        }
        catch
        {
            Directory.CreateDirectory(target);
            File.WriteAllText(Path.Combine(target, ".dev-link"), projectDirectory);
        }

        return target;
    }
}

public sealed class DesktopRecoveryCenterService
{
    private readonly string _contentRoot;
    private readonly LocalAppConfigStore _configStore;

    public DesktopRecoveryCenterService(string contentRoot)
    {
        _contentRoot = contentRoot;
        _configStore = new LocalAppConfigStore(Path.Combine(contentRoot, "content-config.json"));
    }

    public string EnableSafeMode()
    {
        _configStore.Update(config => config.SafeMode = true);
        return "Safe mode enabled. Mods will remain disabled until recovery is complete.";
    }

    public string DisableLastChangedMods()
    {
        _configStore.Update(config =>
        {
            foreach (var modId in config.EnabledSince.OrderByDescending(pair => pair.Value).Take(3).Select(pair => pair.Key).ToArray())
            {
                config.EnabledMods.Remove(modId);
                config.UnsafeMods.Add(modId);
            }
        });
        return "Most recently enabled mods were disabled and marked unsafe.";
    }

    public string ResetPolicies()
    {
        new TrustPolicyEditorService(Path.Combine(_contentRoot, "content-config.json")).ResetPolicies();
        return "Trust policy reset to local defaults.";
    }
}
