namespace TheUnlocker.Modding;

public interface IModContext
{
    string ModsDirectory { get; }

    string ModDirectory { get; }

    IReadOnlySet<string> Permissions { get; }

    CapabilityToken Token { get; }

    IModMenuService MenuItems { get; }

    IAssetRegistry AssetRegistry { get; }

    INotificationService Notifications { get; }

    IModSettingsService Settings { get; }

    IModEventBus Events { get; }

    INavigationService Navigation { get; }

    IAssetImporterRegistry AssetImporters { get; }

    IThemeRegistry Themes { get; }

    ICommandPaletteService CommandPalette { get; }

    IToolPanelRegistry ToolPanels { get; }

    bool HasPermission(string permission);

    void Log(string modId, string message);
}
