namespace TheUnlocker.Modding;

public sealed class ModContext : IModContext
{
    private readonly Action<string> _log;

    public ModContext(
        string modsDirectory,
        string modDirectory,
        IReadOnlySet<string> permissions,
        CapabilityToken token,
        IModMenuService menuItems,
        IAssetRegistry assetRegistry,
        INotificationService notifications,
        IModSettingsService settings,
        IModEventBus events,
        INavigationService navigation,
        IAssetImporterRegistry assetImporters,
        IThemeRegistry themes,
        ICommandPaletteService commandPalette,
        IToolPanelRegistry toolPanels,
        Action<string> log)
    {
        ModsDirectory = modsDirectory;
        ModDirectory = modDirectory;
        Permissions = permissions;
        Token = token;
        MenuItems = menuItems;
        AssetRegistry = assetRegistry;
        Notifications = notifications;
        Settings = settings;
        Events = events;
        Navigation = navigation;
        AssetImporters = assetImporters;
        Themes = themes;
        CommandPalette = commandPalette;
        ToolPanels = toolPanels;
        _log = log;
    }

    public string ModsDirectory { get; }

    public string ModDirectory { get; }

    public IReadOnlySet<string> Permissions { get; }

    public CapabilityToken Token { get; }

    public IModMenuService MenuItems { get; }

    public IAssetRegistry AssetRegistry { get; }

    public INotificationService Notifications { get; }

    public IModSettingsService Settings { get; }

    public IModEventBus Events { get; }

    public INavigationService Navigation { get; }

    public IAssetImporterRegistry AssetImporters { get; }

    public IThemeRegistry Themes { get; }

    public ICommandPaletteService CommandPalette { get; }

    public IToolPanelRegistry ToolPanels { get; }

    public bool HasPermission(string permission)
    {
        return Permissions.Contains(permission);
    }

    public void Log(string modId, string message)
    {
        _log($"[{modId}] {message}");
    }
}
