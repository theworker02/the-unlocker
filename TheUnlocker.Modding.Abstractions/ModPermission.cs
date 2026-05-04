namespace TheUnlocker.Modding;

public static class ModPermission
{
    public const string ReadAssets = "ReadAssets";
    public const string AddMenuItems = "AddMenuItems";
    public const string SendNotifications = "SendNotifications";
    public const string Settings = "Settings";
    public const string Events = "Events";
    public const string Navigation = "Navigation";
    public const string AssetImporters = "AssetImporters";
    public const string Themes = "Themes";
    public const string CommandPalette = "CommandPalette";
    public const string ToolPanels = "ToolPanels";

    public static readonly IReadOnlySet<string> Known = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ReadAssets,
        AddMenuItems,
        SendNotifications,
        Settings,
        Events,
        Navigation,
        AssetImporters,
        Themes,
        CommandPalette,
        ToolPanels
    };
}
