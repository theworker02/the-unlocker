using TheUnlocker.Modding;

namespace TheUnlocker.Modding.TestHarness;

public sealed class FakeModContext : IModContext
{
    private readonly Dictionary<string, string> _settings = new(StringComparer.OrdinalIgnoreCase);

    public string ModsDirectory { get; init; } = Path.GetTempPath();
    public string ModDirectory { get; init; } = Path.GetTempPath();
    public IReadOnlySet<string> Permissions { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    public CapabilityToken Token { get; init; } = new("test-mod", new HashSet<string>(StringComparer.OrdinalIgnoreCase));
    public IModMenuService MenuItems { get; init; } = new FakeMenuService();
    public IAssetRegistry AssetRegistry { get; init; } = new FakeAssetRegistry();
    public INotificationService Notifications { get; init; } = new FakeNotificationService();
    public IModSettingsService Settings { get; }
    public IModEventBus Events { get; init; } = new FakeEventBus();
    public INavigationService Navigation { get; init; } = new FakeNavigationService();
    public IAssetImporterRegistry AssetImporters { get; init; } = new FakeAssetImporterRegistry();
    public IThemeRegistry Themes { get; init; } = new FakeThemeRegistry();
    public ICommandPaletteService CommandPalette { get; init; } = new FakeCommandPaletteService();
    public IToolPanelRegistry ToolPanels { get; init; } = new FakeToolPanelRegistry();
    public List<string> Logs { get; } = [];

    public FakeModContext()
    {
        Settings = new FakeSettingsService(_settings);
    }

    public bool HasPermission(string permission) => Permissions.Contains(permission);
    public void Log(string modId, string message) => Logs.Add($"[{modId}] {message}");
}

file sealed class FakeMenuService : IModMenuService
{
    public IReadOnlyCollection<ModMenuItem> Items => _items;
    private readonly List<ModMenuItem> _items = [];
    public void Add(string title, Action execute) => _items.Add(new ModMenuItem { ModId = "test-mod", Title = title, Execute = execute });
}

file sealed class FakeAssetRegistry : IAssetRegistry
{
    public IReadOnlyCollection<string> Assets => _assets;
    private readonly List<string> _assets = [];
    public void Register(string assetPath) => _assets.Add(assetPath);
}

file sealed class FakeNotificationService : INotificationService
{
    public IReadOnlyCollection<string> Notifications => _messages;
    private readonly List<string> _messages = [];
    public void Show(string message) => _messages.Add(message);
}

file sealed class FakeSettingsService : IModSettingsService
{
    private readonly Dictionary<string, string> _settings;
    public FakeSettingsService(Dictionary<string, string> settings) => _settings = settings;
    public string Get(string key, string fallback = "") => _settings.TryGetValue(key, out var value) ? value : fallback;
    public void Set(string key, string value) => _settings[key] = value;
}

file sealed class FakeEventBus : IModEventBus
{
    public IReadOnlyCollection<string> RegisteredSchemas => _schemas;
    private readonly List<string> _schemas = [];
    public List<object?> Published { get; } = [];
    public void Publish<TEvent>(TEvent eventData) => Published.Add(eventData);
    public void Subscribe<TEvent>(Action<TEvent> handler) { }
    public void RegisterSchema(string eventName, Version version) => _schemas.Add($"{eventName}@{version}");
}

file sealed class FakeNavigationService : INavigationService
{
    public IReadOnlyCollection<string> Items => _items;
    private readonly List<string> _items = [];
    public void Register(string title) => _items.Add(title);
}

file sealed class FakeAssetImporterRegistry : IAssetImporterRegistry
{
    public IReadOnlyCollection<string> Importers => _importers;
    private readonly List<string> _importers = [];
    public void Register(string extension, string displayName) => _importers.Add($"{extension}:{displayName}");
}

file sealed class FakeThemeRegistry : IThemeRegistry
{
    public IReadOnlyCollection<string> Themes => _themes;
    private readonly List<string> _themes = [];
    public void Register(string themeName) => _themes.Add(themeName);
}

file sealed class FakeCommandPaletteService : ICommandPaletteService
{
    public IReadOnlyCollection<string> Commands => _commands;
    private readonly List<string> _commands = [];
    public void Register(string commandName, Action execute) => _commands.Add(commandName);
    public void Register(string commandName, string scope, Action execute) => _commands.Add($"{scope}:{commandName}");
}

file sealed class FakeToolPanelRegistry : IToolPanelRegistry
{
    public IReadOnlyCollection<string> Panels => _panels;
    private readonly List<string> _panels = [];
    public void Register(string panelName) => _panels.Add(panelName);
}
