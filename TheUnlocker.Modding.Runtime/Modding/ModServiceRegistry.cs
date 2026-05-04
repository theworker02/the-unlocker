using TheUnlocker.Configuration;

namespace TheUnlocker.Modding;

public sealed class ModServiceRegistry
{
    private readonly List<ModMenuItem> _menuItems = new();
    private readonly List<string> _assets = new();
    private readonly List<string> _notifications = new();
    private readonly List<string> _navigationItems = new();
    private readonly List<string> _assetImporters = new();
    private readonly List<string> _themes = new();
    private readonly List<string> _commands = new();
    private readonly List<string> _toolPanels = new();
    private readonly LocalAppConfigStore _configStore;

    public ModServiceRegistry(LocalAppConfigStore configStore)
    {
        _configStore = configStore;
    }

    public IReadOnlyCollection<ModMenuItem> MenuItems => _menuItems;

    public IReadOnlyCollection<string> Assets => _assets;

    public IReadOnlyCollection<string> Notifications => _notifications;

    public IModMenuService CreateMenuService(CapabilityToken token)
    {
        return new ModMenuService(token, _menuItems);
    }

    public IAssetRegistry CreateAssetRegistry(CapabilityToken token)
    {
        return new AssetRegistry(token, _assets);
    }

    public INotificationService CreateNotificationService(CapabilityToken token)
    {
        return new NotificationService(token, _notifications);
    }

    public IModSettingsService CreateSettingsService(CapabilityToken token)
    {
        return new ModSettingsService(token, _configStore);
    }

    public INavigationService CreateNavigationService(CapabilityToken token)
    {
        return new TextRegistryService(token, ModPermission.Navigation, _navigationItems);
    }

    public IAssetImporterRegistry CreateAssetImporterRegistry(CapabilityToken token)
    {
        return new AssetImporterRegistry(token, _assetImporters);
    }

    public IThemeRegistry CreateThemeRegistry(CapabilityToken token)
    {
        return new ThemeRegistry(token, _themes);
    }

    public ICommandPaletteService CreateCommandPalette(CapabilityToken token, Action? onCommandAdded = null)
    {
        return new CommandPaletteService(token, _commands, onCommandAdded);
    }

    public IToolPanelRegistry CreateToolPanelRegistry(CapabilityToken token)
    {
        return new ToolPanelRegistry(token, _toolPanels);
    }

    private static void Demand(CapabilityToken token, string permission)
    {
        if (!token.Permissions.Contains(permission))
        {
            throw new UnauthorizedAccessException($"Mod does not declare required permission: {permission}");
        }
    }

    private sealed class ModMenuService : IModMenuService
    {
        private readonly CapabilityToken _token;
        private readonly List<ModMenuItem> _items;

        public ModMenuService(CapabilityToken token, List<ModMenuItem> items)
        {
            _token = token;
            _items = items;
        }

        public IReadOnlyCollection<ModMenuItem> Items => _items;

        public void Add(string title, Action execute)
        {
            Demand(_token, ModPermission.AddMenuItems);
            _items.Add(new ModMenuItem { ModId = _token.ModId, Title = title, Execute = execute });
        }
    }

    private sealed class AssetRegistry : IAssetRegistry
    {
        private readonly CapabilityToken _token;
        private readonly List<string> _assets;

        public AssetRegistry(CapabilityToken token, List<string> assets)
        {
            _token = token;
            _assets = assets;
        }

        public IReadOnlyCollection<string> Assets => _assets;

        public void Register(string assetPath)
        {
            Demand(_token, ModPermission.ReadAssets);
            _assets.Add($"{_token.ModId}:{assetPath}");
        }
    }

    private sealed class NotificationService : INotificationService
    {
        private readonly CapabilityToken _token;
        private readonly List<string> _notifications;

        public NotificationService(CapabilityToken token, List<string> notifications)
        {
            _token = token;
            _notifications = notifications;
        }

        public IReadOnlyCollection<string> Notifications => _notifications;

        public void Show(string message)
        {
            Demand(_token, ModPermission.SendNotifications);
            _notifications.Add($"[{_token.ModId}] {message}");
        }
    }

    private sealed class ModSettingsService : IModSettingsService
    {
        private readonly CapabilityToken _token;
        private readonly LocalAppConfigStore _configStore;

        public ModSettingsService(CapabilityToken token, LocalAppConfigStore configStore)
        {
            _token = token;
            _configStore = configStore;
        }

        public string Get(string key, string fallback = "")
        {
            Demand(_token, ModPermission.Settings);
            var config = _configStore.Load();
            return config.ModSettings.TryGetValue(_token.ModId, out var settings)
                && settings.TryGetValue(key, out var value)
                    ? value
                    : fallback;
        }

        public void Set(string key, string value)
        {
            Demand(_token, ModPermission.Settings);
            _configStore.Update(config =>
            {
                if (!config.ModSettings.TryGetValue(_token.ModId, out var settings))
                {
                    settings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    config.ModSettings[_token.ModId] = settings;
                }

                settings[key] = value;
            });
        }
    }

    private sealed class TextRegistryService : INavigationService
    {
        private readonly CapabilityToken _token;
        private readonly string _permission;
        private readonly List<string> _items;

        public TextRegistryService(CapabilityToken token, string permission, List<string> items)
        {
            _token = token;
            _permission = permission;
            _items = items;
        }

        public IReadOnlyCollection<string> Items => _items;

        public void Register(string title)
        {
            Demand(_token, _permission);
            _items.Add($"{_token.ModId}:{title}");
        }
    }

    private sealed class AssetImporterRegistry : IAssetImporterRegistry
    {
        private readonly CapabilityToken _token;
        private readonly List<string> _items;

        public AssetImporterRegistry(CapabilityToken token, List<string> items)
        {
            _token = token;
            _items = items;
        }

        public IReadOnlyCollection<string> Importers => _items;

        public void Register(string extension, string displayName)
        {
            Demand(_token, ModPermission.AssetImporters);
            _items.Add($"{_token.ModId}:{extension}:{displayName}");
        }
    }

    private sealed class ThemeRegistry : IThemeRegistry
    {
        private readonly CapabilityToken _token;
        private readonly List<string> _items;

        public ThemeRegistry(CapabilityToken token, List<string> items)
        {
            _token = token;
            _items = items;
        }

        public IReadOnlyCollection<string> Themes => _items;

        public void Register(string themeName)
        {
            Demand(_token, ModPermission.Themes);
            _items.Add($"{_token.ModId}:{themeName}");
        }
    }

    private sealed class CommandPaletteService : ICommandPaletteService
    {
        private readonly CapabilityToken _token;
        private readonly List<string> _items;
        private readonly Action? _onCommandAdded;

        public CommandPaletteService(CapabilityToken token, List<string> items, Action? onCommandAdded)
        {
            _token = token;
            _items = items;
            _onCommandAdded = onCommandAdded;
        }

        public IReadOnlyCollection<string> Commands => _items;

        public void Register(string commandName, Action execute)
        {
            Register(commandName, "default", execute);
        }

        public void Register(string commandName, string scope, Action execute)
        {
            Demand(_token, ModPermission.CommandPalette);
            _items.Add($"{_token.ModId}:{scope}:{commandName}");
            _onCommandAdded?.Invoke();
        }
    }

    private sealed class ToolPanelRegistry : IToolPanelRegistry
    {
        private readonly CapabilityToken _token;
        private readonly List<string> _items;

        public ToolPanelRegistry(CapabilityToken token, List<string> items)
        {
            _token = token;
            _items = items;
        }

        public IReadOnlyCollection<string> Panels => _items;

        public void Register(string panelName)
        {
            Demand(_token, ModPermission.ToolPanels);
            _items.Add($"{_token.ModId}:{panelName}");
        }
    }
}
