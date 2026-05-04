using System.IO;
using System.Text.Json;
using System.Windows;
using TheUnlocker.Desktop;

namespace TheUnlocker;

public partial class FirstRunWindow : Window
{
    private readonly string _setupPath;

    public FirstRunWindow(string contentRoot)
    {
        InitializeComponent();
        _setupPath = Path.Combine(contentRoot, "first-run.json");
        ModsFolderBox.Text = Path.Combine(contentRoot, "Mods");
    }

    public static bool NeedsSetup(string contentRoot)
    {
        return !File.Exists(Path.Combine(contentRoot, "first-run.json"));
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_setupPath)!);
        var state = new FirstRunSetupState
        {
            HasCompletedSetup = true,
            ModsDirectory = ModsFolderBox.Text,
            RegistryUrl = RegistryUrlBox.Text,
            SafeMode = SafeModeBox.IsChecked == true,
            AllowUnsignedMods = UnsignedBox.IsChecked == true
        };
        File.WriteAllText(_setupPath, JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true }));
        DialogResult = true;
    }
}
