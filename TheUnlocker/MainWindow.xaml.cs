using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using TheUnlocker.ContentManagement;
using TheUnlocker.Modding;
using TheUnlocker.Protocol;
using TheUnlocker.ViewModels;

namespace TheUnlocker;

public partial class MainWindow : Window
{
    private readonly DispatcherTimer _refreshDebounce;
    private readonly List<FileSystemWatcher> _watchers = new();

    public MainWindow()
    {
        InitializeComponent();

        var contentRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "TheUnlocker");

        if (FirstRunWindow.NeedsSetup(contentRoot))
        {
            new FirstRunWindow(contentRoot) { Owner = this }.ShowDialog();
        }

        var contentManager = new ContentManager(
            modulesDirectory: Path.Combine(contentRoot, "Modules"),
            configPath: Path.Combine(contentRoot, "content-config.json"));

        var modLoader = new ModLoader(
            modsDirectory: Path.Combine(contentRoot, "Mods"),
            configPath: Path.Combine(contentRoot, "content-config.json"),
            logsDirectory: Path.Combine(contentRoot, "Logs"),
            appVersion: typeof(App).Assembly.GetName().Version ?? new Version(1, 0, 0));

        var viewModel = new MainWindowViewModel(contentManager, modLoader);
        DataContext = viewModel;

        foreach (var argument in Environment.GetCommandLineArgs().Skip(1))
        {
            var installId = new ProtocolRegistration().TryParseInstallUri(argument);
            if (!string.IsNullOrWhiteSpace(installId))
            {
                viewModel.InstallMarketplaceMod(installId);
            }

            var packId = new ProtocolRegistration().TryParseInstallPackUri(argument);
            if (!string.IsNullOrWhiteSpace(packId))
            {
                viewModel.InstallModpack(packId);
            }
        }

        _refreshDebounce = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };
        _refreshDebounce.Tick += (_, _) =>
        {
            _refreshDebounce.Stop();
            viewModel.Refresh();
        };

        WatchDirectory(Path.Combine(contentRoot, "Modules"));
        WatchDirectory(Path.Combine(contentRoot, "Mods"));
        Closed += (_, _) =>
        {
            foreach (var watcher in _watchers)
            {
                watcher.Dispose();
            }
        };
    }

    private void WatchDirectory(string path)
    {
        Directory.CreateDirectory(path);
        var watcher = new FileSystemWatcher(path)
        {
            IncludeSubdirectories = true,
            EnableRaisingEvents = true
        };

        FileSystemEventHandler refresh = (_, _) => Dispatcher.Invoke(() =>
        {
            _refreshDebounce.Stop();
            _refreshDebounce.Start();
        });

        watcher.Changed += refresh;
        watcher.Created += refresh;
        watcher.Deleted += refresh;
        watcher.Renamed += (_, _) => Dispatcher.Invoke(() =>
        {
            _refreshDebounce.Stop();
            _refreshDebounce.Start();
        });
        _watchers.Add(watcher);
    }

    private void OnModDrop(object sender, DragEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        if (e.Data.GetData(DataFormats.FileDrop) is not string[] files)
        {
            return;
        }

        foreach (var file in files.Where(file => file.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)
            || file.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)))
        {
            viewModel.ImportModFile(file);
        }
    }

    private void OnAccountPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (sender is PasswordBox box && DataContext is MainWindowViewModel viewModel)
        {
            viewModel.AccountPassword = box.Password;
        }
    }
}
