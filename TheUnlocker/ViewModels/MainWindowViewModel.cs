using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using TheUnlocker.ContentManagement;
using TheUnlocker.Desktop;
using TheUnlocker.Observability;
using TheUnlocker.Modding;
using TheUnlocker.Protocol;
using TheUnlocker.RegistryClient;

namespace TheUnlocker.ViewModels;

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private readonly IContentManager _contentManager;
    private readonly IModLoader _modLoader;
    private readonly string _contentRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TheUnlocker");
    private string _statusMessage = "";
    private string _featureGateMessage = "";
    private string _modStatusMessage = "";
    private string _importStatusMessage = "";
    private string _registryApiUrl = "http://localhost:8088";
    private string _accountEmail = "";
    private string _accountPassword = "";
    private string _accountStatusMessage = "Not signed in.";
    private string _selectedProfile = "Default";
    private MarketplaceModViewModel? _selectedMarketplaceMod;
    private ModUpdateInfo? _selectedUpdate;
    private LoadedModViewModel? _selectedLoadedMod;
    private ModRegistryEntry? _selectedRollbackEntry;
    private string _logSearchText = "";

    public MainWindowViewModel(IContentManager contentManager, IModLoader modLoader)
    {
        _contentManager = contentManager;
        _modLoader = modLoader;
        RefreshCommand = new RelayCommand(_ => Refresh());
        ImportModCommand = new RelayCommand(_ => ImportMod());
        SaveProfileCommand = new RelayCommand(_ => SaveProfile());
        InstallMarketplaceModCommand = new RelayCommand(_ => InstallMarketplaceMod(), _ => SelectedMarketplaceMod is not null);
        ExportDiagnosticsCommand = new RelayCommand(_ => ExportDiagnostics());
        InstallUpdateCommand = new RelayCommand(_ => InstallSelectedUpdate(), _ => SelectedUpdate is not null);
        ExportCompatibilityReportCommand = new RelayCommand(_ => ExportCompatibilityReport());
        GenerateModDocsCommand = new RelayCommand(_ => GenerateModDocs(), _ => SelectedLoadedMod is not null);
        BackupCommand = new RelayCommand(_ => Backup());
        RestoreCommand = new RelayCommand(_ => Restore());
        RollbackCommand = new RelayCommand(_ => RollbackSelected(), _ => SelectedRollbackEntry is not null);
        ExportGraphCommand = new RelayCommand(_ => ExportGraph());
        UploadCrashCommand = new RelayCommand(_ => UploadCrash());
        ToggleUnsignedPolicyCommand = new RelayCommand(_ => ToggleUnsignedPolicy());
        ResetPoliciesCommand = new RelayCommand(_ => ResetPolicies());
        EnableSafeModeCommand = new RelayCommand(_ => EnableSafeMode());
        DisableLastChangedModsCommand = new RelayCommand(_ => DisableLastChangedMods());
        LinkDevModCommand = new RelayCommand(_ => LinkDevMod());
        SignInCommand = new RelayCommand(async _ => await SignInAsync());
        SignOutCommand = new RelayCommand(_ => SignOut());
        RegisterProtocolCommand = new RelayCommand(_ => RegisterProtocol());
        ProcessInstallQueueCommand = new RelayCommand(async _ => await ProcessInstallQueueAsync());
        Refresh();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<ModuleViewModel> Modules { get; } = new();

    public ObservableCollection<LoadedModViewModel> LoadedMods { get; } = new();

    public ObservableCollection<ModSettingViewModel> ModSettings { get; } = new();

    public ObservableCollection<string> ModLogs { get; } = new();

    public ObservableCollection<ModLogEntryViewModel> LogEntries { get; } = new();

    public ObservableCollection<ModLoadOrderInfo> LoadOrder { get; } = new();

    public ObservableCollection<ModConflictInfo> Conflicts { get; } = new();

    public ObservableCollection<ModHealthInfo> Health { get; } = new();

    public ObservableCollection<ModUpdateInfo> Updates { get; } = new();

    public ObservableCollection<MarketplaceModViewModel> Marketplace { get; } = new();

    public ObservableCollection<string> Profiles { get; } = new();

    public ObservableCollection<NotificationItem> Notifications { get; } = new();

    public ObservableCollection<ExtensionContributionInfo> ExtensionGallery { get; } = new();

    public ObservableCollection<InstallQueueItem> InstallQueue { get; } = new();

    public ObservableCollection<PerGameDashboard> GameDashboards { get; } = new();

    public ObservableCollection<ModRegistryEntry> RollbackHistory { get; } = new();

    public ObservableCollection<PermissionTimelineEntry> PermissionTimeline { get; } = new();

    public ObservableCollection<ModDiffResult> ModDiffs { get; } = new();

    public ObservableCollection<ModLogEntryViewModel> FilteredLogEntries { get; } = new();

    public ObservableCollection<string> RiskDashboard { get; } = new();

    public ObservableCollection<string> TrustPolicyLines { get; } = new();

    public ObservableCollection<string> PermissionSimulationReports { get; } = new();

    public ObservableCollection<string> RecoveryActions { get; } = new();

    public ObservableCollection<string> MajorPlatformUpgrades { get; } = new();

    public ICommand RefreshCommand { get; }

    public ICommand ImportModCommand { get; }

    public ICommand SaveProfileCommand { get; }

    public ICommand InstallMarketplaceModCommand { get; }

    public ICommand ExportDiagnosticsCommand { get; }

    public ICommand InstallUpdateCommand { get; }

    public ICommand ExportCompatibilityReportCommand { get; }

    public ICommand GenerateModDocsCommand { get; }

    public ICommand BackupCommand { get; }

    public ICommand RestoreCommand { get; }
    public ICommand RollbackCommand { get; }
    public ICommand ExportGraphCommand { get; }
    public ICommand UploadCrashCommand { get; }
    public ICommand ToggleUnsignedPolicyCommand { get; }
    public ICommand ResetPoliciesCommand { get; }
    public ICommand EnableSafeModeCommand { get; }
    public ICommand DisableLastChangedModsCommand { get; }
    public ICommand LinkDevModCommand { get; }
    public ICommand SignInCommand { get; }
    public ICommand SignOutCommand { get; }
    public ICommand RegisterProtocolCommand { get; }
    public ICommand ProcessInstallQueueCommand { get; }

    public string RegistryApiUrl
    {
        get => _registryApiUrl;
        set
        {
            _registryApiUrl = value;
            OnPropertyChanged();
        }
    }

    public string AccountEmail
    {
        get => _accountEmail;
        set
        {
            _accountEmail = value;
            OnPropertyChanged();
        }
    }

    public string AccountPassword
    {
        get => _accountPassword;
        set
        {
            _accountPassword = value;
            OnPropertyChanged();
        }
    }

    public string AccountStatusMessage
    {
        get => _accountStatusMessage;
        private set
        {
            _accountStatusMessage = value;
            OnPropertyChanged();
        }
    }

    public ModRegistryEntry? SelectedRollbackEntry
    {
        get => _selectedRollbackEntry;
        set
        {
            _selectedRollbackEntry = value;
            OnPropertyChanged();
            if (RollbackCommand is RelayCommand command)
            {
                command.RaiseCanExecuteChanged();
            }
        }
    }

    public string LogSearchText
    {
        get => _logSearchText;
        set
        {
            _logSearchText = value;
            OnPropertyChanged();
            RefreshLogSearch();
        }
    }

    public MarketplaceModViewModel? SelectedMarketplaceMod
    {
        get => _selectedMarketplaceMod;
        set
        {
            _selectedMarketplaceMod = value;
            OnPropertyChanged();
            if (InstallMarketplaceModCommand is RelayCommand command)
            {
                command.RaiseCanExecuteChanged();
            }
        }
    }

    public ModUpdateInfo? SelectedUpdate
    {
        get => _selectedUpdate;
        set
        {
            _selectedUpdate = value;
            OnPropertyChanged();
            if (InstallUpdateCommand is RelayCommand command)
            {
                command.RaiseCanExecuteChanged();
            }
        }
    }

    public LoadedModViewModel? SelectedLoadedMod
    {
        get => _selectedLoadedMod;
        set
        {
            _selectedLoadedMod = value;
            OnPropertyChanged();
            if (GenerateModDocsCommand is RelayCommand command)
            {
                command.RaiseCanExecuteChanged();
            }
        }
    }

    public string SelectedProfile
    {
        get => _selectedProfile;
        set
        {
            if (_selectedProfile == value || string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            _selectedProfile = value;
            OnPropertyChanged();
            _modLoader.SetActiveProfile(value);
            RefreshCodeMods();
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set
        {
            _statusMessage = value;
            OnPropertyChanged();
        }
    }

    public string FeatureGateMessage
    {
        get => _featureGateMessage;
        private set
        {
            _featureGateMessage = value;
            OnPropertyChanged();
        }
    }

    public string ModStatusMessage
    {
        get => _modStatusMessage;
        private set
        {
            _modStatusMessage = value;
            OnPropertyChanged();
        }
    }

    public string ImportStatusMessage
    {
        get => _importStatusMessage;
        private set
        {
            _importStatusMessage = value;
            OnPropertyChanged();
        }
    }

    public void Refresh()
    {
        RefreshContentModules();
        RefreshCodeMods();
    }

    private void RefreshContentModules()
    {
        _contentManager.Refresh();
        Modules.Clear();

        foreach (var module in _contentManager.Modules.OrderBy(module => module.Name))
        {
            Modules.Add(new ModuleViewModel(module, SetModuleEnabled));
        }

        var activeCount = Modules.Count(module => module.IsEnabled);
        var issueCount = Modules.Count(module => module.Status is "Missing" or "Error");

        StatusMessage = $"{Modules.Count} discovered, {activeCount} active, {issueCount} need attention.";
        FeatureGateMessage = _contentManager.IsModuleEnabled("extra-textures")
            ? "Gate check: extra-textures is enabled, so the app can initialize that feature."
            : "Gate check: extra-textures is not enabled; the app continues without that feature.";
    }

    private void RefreshCodeMods()
    {
        _modLoader.LoadMods();

        LoadedMods.Clear();
        foreach (var mod in _modLoader.LoadedMods.OrderBy(mod => mod.Name))
        {
            LoadedMods.Add(new LoadedModViewModel(mod, SetModEnabled));
        }

        ModSettings.Clear();
        foreach (var setting in _modLoader.ModSettings.OrderBy(setting => setting.ModId).ThenBy(setting => setting.Label))
        {
            ModSettings.Add(new ModSettingViewModel(setting, SetModSetting));
        }

        ModLogs.Clear();
        foreach (var log in _modLoader.Logs)
        {
            ModLogs.Add(log);
        }

        LogEntries.Clear();
        foreach (var log in _modLoader.LogEntries.OrderByDescending(log => log.Timestamp).Take(500))
        {
            LogEntries.Add(new ModLogEntryViewModel(log));
        }
        RefreshLogSearch();

        LoadOrder.Clear();
        foreach (var item in _modLoader.LoadOrder)
        {
            LoadOrder.Add(item);
        }

        Conflicts.Clear();
        foreach (var item in _modLoader.Conflicts)
        {
            Conflicts.Add(item);
        }

        Health.Clear();
        foreach (var item in _modLoader.Health)
        {
            Health.Add(item);
        }

        Updates.Clear();
        foreach (var item in _modLoader.Updates)
        {
            Updates.Add(item);
        }

        Marketplace.Clear();
        foreach (var item in _modLoader.Marketplace)
        {
            Marketplace.Add(new MarketplaceModViewModel(item));
        }

        RefreshPlatformCollections();
        RefreshAdvancedCollections();

        Profiles.Clear();
        foreach (var profile in _modLoader.Profiles)
        {
            Profiles.Add(profile);
        }

        _selectedProfile = _modLoader.ActiveProfile;
        OnPropertyChanged(nameof(SelectedProfile));

        var loadedCount = LoadedMods.Count(mod => mod.Status == "Loaded");
        var issueCount = LoadedMods.Count(mod => mod.Status is "Skipped" or "Error");
        ModStatusMessage = $"{LoadedMods.Count} mod entries found, {loadedCount} loaded, {issueCount} skipped or failed.";
    }

    private void RefreshAdvancedCollections()
    {
        RollbackHistory.Clear();
        foreach (var item in _modLoader.RegistryEntries.OrderByDescending(item => item.InstalledAt))
        {
            RollbackHistory.Add(item);
        }

        PermissionTimeline.Clear();
        foreach (var mod in _modLoader.LoadedMods)
        {
            PermissionTimeline.Add(new PermissionTimelineEntry
            {
                ModId = mod.Id,
                Version = mod.Version,
                AddedPermissions = mod.Permissions,
                RemovedPermissions = []
            });
        }

        ModDiffs.Clear();
        foreach (var update in _modLoader.Updates)
        {
            ModDiffs.Add(new ModDiffResult
            {
                ModId = update.ModId,
                FromVersion = update.InstalledVersion,
                ToVersion = update.AvailableVersion,
                PermissionChanges = string.IsNullOrWhiteSpace(update.NewPermissions) ? [] : [$"+ {update.NewPermissions}"],
                Changelog = update.Changelog
            });
        }

        InstallQueue.Clear();
        foreach (var update in _modLoader.Updates)
        {
            InstallQueue.Add(new InstallQueueItem
            {
                Source = update.DownloadUrl,
                Status = "Ready",
                Attempts = 0
            });
        }

        RiskDashboard.Clear();
        PermissionSimulationReports.Clear();
        foreach (var mod in _modLoader.LoadedMods)
        {
            RiskDashboard.Add(new TrustSimulationService().Simulate(mod.Id, mod.Permissions, mod.Targets));
            PermissionSimulationReports.Add(new PermissionSimulator().Simulate(new ModManifest
            {
                Id = mod.Id,
                Permissions = mod.Permissions,
                Targets = mod.Targets
            }).ToString());
        }

        RefreshTrustPolicy();
        RefreshRecoveryActions();
        RefreshMajorPlatformUpgrades();
    }

    private void RefreshLogSearch()
    {
        FilteredLogEntries.Clear();
        var results = new AdvancedLogSearch().Search(_modLoader.LogEntries, new LogSearchQuery { Text = LogSearchText });
        foreach (var item in results.Take(500))
        {
            FilteredLogEntries.Add(new ModLogEntryViewModel(item));
        }
    }

    private void RefreshPlatformCollections()
    {
        Notifications.Clear();
        foreach (var conflict in _modLoader.Conflicts)
        {
            Notifications.Add(new NotificationItem
            {
                Severity = "Warning",
                Source = "Conflicts",
                Message = $"{conflict.Target}: {conflict.ModIds}"
            });
        }

        foreach (var update in _modLoader.Updates)
        {
            Notifications.Add(new NotificationItem
            {
                Severity = string.IsNullOrWhiteSpace(update.NewPermissions) ? "Info" : "Warning",
                Source = "Updates",
                Message = $"{update.ModId} {update.InstalledVersion} -> {update.AvailableVersion}"
            });
        }

        foreach (var health in _modLoader.Health.Where(item => !string.IsNullOrWhiteSpace(item.LastError)))
        {
            Notifications.Add(new NotificationItem
            {
                Severity = "Error",
                Source = health.ModId,
                Message = health.LastError
            });
        }

        ExtensionGallery.Clear();
        foreach (var mod in _modLoader.LoadedMods)
        {
            ExtensionGallery.Add(new ExtensionContributionInfo
            {
                ModId = mod.Id,
                Menus = mod.Permissions.Contains("AddMenuItems", StringComparer.OrdinalIgnoreCase) ? "declared" : "",
                Commands = mod.Permissions.Contains("AddCommands", StringComparer.OrdinalIgnoreCase) ? "declared" : "",
                Themes = mod.Permissions.Contains("AddThemes", StringComparer.OrdinalIgnoreCase) ? "declared" : "",
                Panels = mod.Permissions.Contains("AddToolPanels", StringComparer.OrdinalIgnoreCase) ? "declared" : "",
                Importers = mod.Permissions.Contains("RegisterAssetImporters", StringComparer.OrdinalIgnoreCase) ? "declared" : ""
            });
        }

        GameDashboards.Clear();
        GameDashboards.Add(new PerGameDashboard
        {
            GameId = "local",
            Profile = _modLoader.ActiveProfile,
            InstalledMods = _modLoader.LoadedMods.Count,
            Conflicts = _modLoader.Conflicts.Count,
            Warnings = Notifications.Count(item => item.Severity is "Warning" or "Error")
        });
    }

    private void SetModuleEnabled(string moduleId, bool enabled)
    {
        _contentManager.SetModuleEnabled(moduleId, enabled);
        RefreshContentModules();
    }

    private void SetModEnabled(string modId, bool enabled)
    {
        if (enabled)
        {
            var mod = LoadedMods.FirstOrDefault(item => item.Id.Equals(modId, StringComparison.OrdinalIgnoreCase));
            if (mod is not null && !mod.Permissions.Equals("none", StringComparison.OrdinalIgnoreCase))
            {
                var result = MessageBox.Show(
                    $"Enable '{mod.Name}' with these permissions?\n\n{mod.Permissions}",
                    "Approve Mod Permissions",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes)
                {
                    RefreshCodeMods();
                    return;
                }
            }
        }

        _modLoader.SetModEnabled(modId, enabled);
        RefreshCodeMods();
    }

    private void SetModSetting(string modId, string key, string value)
    {
        _modLoader.SetModSetting(modId, key, value);
        RefreshCodeMods();
    }

    private void ImportMod()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Mod packages (*.zip;*.dll)|*.zip;*.dll|All files (*.*)|*.*",
            Multiselect = false,
            Title = "Import Mod"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            ImportStatusMessage = _modLoader.InstallMod(dialog.FileName);
            RefreshCodeMods();
        }
        catch (Exception ex)
        {
            ImportStatusMessage = $"Import failed: {ex.Message}";
        }
    }

    public void ImportModFile(string path)
    {
        try
        {
            ImportStatusMessage = _modLoader.InstallMod(path);
            RefreshCodeMods();
        }
        catch (Exception ex)
        {
            ImportStatusMessage = $"Import failed: {ex.Message}";
        }
    }

    public void InstallMarketplaceMod(string modId)
    {
        try
        {
            ImportStatusMessage = _modLoader.InstallMarketplaceMod(modId);
            RefreshCodeMods();
        }
        catch (Exception ex)
        {
            ImportStatusMessage = $"Marketplace install failed: {ex.Message}";
        }
    }

    public void InstallModpack(string lockfileUrlOrPath)
    {
        try
        {
            ImportStatusMessage = _modLoader.InstallModpack(lockfileUrlOrPath);
            RefreshCodeMods();
        }
        catch (Exception ex)
        {
            ImportStatusMessage = $"Modpack install failed: {ex.Message}";
        }
    }

    private void SaveProfile()
    {
        _modLoader.SaveProfile(SelectedProfile);
        RefreshCodeMods();
    }

    private void InstallMarketplaceMod()
    {
        if (SelectedMarketplaceMod is null)
        {
            return;
        }

        try
        {
            ImportStatusMessage = _modLoader.InstallMarketplaceMod(SelectedMarketplaceMod.Id);
            RefreshCodeMods();
        }
        catch (Exception ex)
        {
            ImportStatusMessage = $"Marketplace install failed: {ex.Message}";
        }
    }

    private void ExportDiagnostics()
    {
        try
        {
            var output = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "TheUnlocker",
                "Diagnostics");
            ImportStatusMessage = $"Diagnostics exported: {_modLoader.ExportDiagnostics(output)}";
        }
        catch (Exception ex)
        {
            ImportStatusMessage = $"Diagnostics export failed: {ex.Message}";
        }
    }

    private void InstallSelectedUpdate()
    {
        if (SelectedUpdate is null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(SelectedUpdate.NewPermissions))
        {
            var result = MessageBox.Show(
                $"The update requests new permissions:\n\n{SelectedUpdate.NewPermissions}\n\nInstall anyway?",
                "Permission Changes",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes)
            {
                return;
            }
        }

        try
        {
            ImportStatusMessage = _modLoader.InstallMarketplaceMod(SelectedUpdate.ModId);
            RefreshCodeMods();
        }
        catch (Exception ex)
        {
            ImportStatusMessage = $"Update failed: {ex.Message}";
        }
    }

    private void ExportCompatibilityReport()
    {
        try
        {
            var output = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TheUnlocker", "Reports");
            ImportStatusMessage = $"Compatibility report: {_modLoader.GenerateCompatibilityReport(output)}";
        }
        catch (Exception ex)
        {
            ImportStatusMessage = $"Compatibility report failed: {ex.Message}";
        }
    }

    private void GenerateModDocs()
    {
        if (SelectedLoadedMod is null)
        {
            return;
        }

        try
        {
            var output = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TheUnlocker", "Docs");
            ImportStatusMessage = $"Mod docs: {_modLoader.GenerateModDocumentation(SelectedLoadedMod.Id, output)}";
        }
        catch (Exception ex)
        {
            ImportStatusMessage = $"Mod docs failed: {ex.Message}";
        }
    }

    private void Backup()
    {
        try
        {
            var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TheUnlocker");
            var output = Path.Combine(root, "Backups", $"backup-{DateTime.Now:yyyyMMdd-HHmmss}.zip");
            ImportStatusMessage = $"Backup exported: {new BackupRestoreService().Export(root, output)}";
        }
        catch (Exception ex)
        {
            ImportStatusMessage = $"Backup failed: {ex.Message}";
        }
    }

    private void Restore()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "TheUnlocker backup (*.zip)|*.zip|All files (*.*)|*.*",
            Title = "Restore Backup"
        };
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TheUnlocker");
            new BackupRestoreService().Restore(dialog.FileName, root);
            ImportStatusMessage = "Backup restored.";
            Refresh();
        }
        catch (Exception ex)
        {
            ImportStatusMessage = $"Restore failed: {ex.Message}";
        }
    }

    private void RollbackSelected()
    {
        if (SelectedRollbackEntry is null)
        {
            return;
        }

        try
        {
            ImportStatusMessage = _modLoader.RollbackMod(SelectedRollbackEntry.Id, SelectedRollbackEntry.PackagePath);
            RefreshCodeMods();
        }
        catch (Exception ex)
        {
            ImportStatusMessage = $"Rollback failed: {ex.Message}";
        }
    }

    private void ExportGraph()
    {
        try
        {
            var output = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TheUnlocker", "Reports", "dependency-graph.mmd");
            ImportStatusMessage = $"Dependency graph exported: {_modLoader.ExportDependencyGraph(output)}";
        }
        catch (Exception ex)
        {
            ImportStatusMessage = $"Graph export failed: {ex.Message}";
        }
    }

    private void UploadCrash()
    {
        try
        {
            var output = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TheUnlocker", "Diagnostics");
            var bundle = _modLoader.ExportDiagnostics(output);
            ImportStatusMessage = $"Crash bundle prepared for upload: {bundle}";
        }
        catch (Exception ex)
        {
            ImportStatusMessage = $"Crash upload failed: {ex.Message}";
        }
    }

    private void RefreshTrustPolicy()
    {
        TrustPolicyLines.Clear();
        var config = new TrustPolicyEditorService(Path.Combine(_contentRoot, "content-config.json")).Load();
        TrustPolicyLines.Add($"Unsigned mods: {(config.Policy.AllowUnsignedMods ? "allowed" : "blocked")}");
        TrustPolicyLines.Add($"Allowed publishers: {string.Join(", ", config.Policy.AllowedPublishers.DefaultIfEmpty("any"))}");
        TrustPolicyLines.Add($"Blocked mods: {string.Join(", ", config.Policy.BlockedMods.DefaultIfEmpty("none"))}");
        TrustPolicyLines.Add($"Private registries: {string.Join(", ", config.Policy.PrivateRegistryUrls.DefaultIfEmpty("none"))}");
        TrustPolicyLines.Add($"Permission defaults: {string.Join(", ", config.Policy.PermissionDefaults.Select(pair => $"{pair.Key}={pair.Value}").DefaultIfEmpty("none"))}");
    }

    private void RefreshRecoveryActions()
    {
        RecoveryActions.Clear();
        RecoveryActions.Add("Safe mode disables mods while keeping content modules visible.");
        RecoveryActions.Add("Disable last changed mods marks recently enabled mods as unsafe.");
        RecoveryActions.Add("Rollback restores a selected package from the Rollback tab.");
        RecoveryActions.Add("Diagnostics exports logs, manifests, config, and health data.");
        RecoveryActions.Add("Reset policies returns trust settings to local defaults.");
    }

    private void RefreshMajorPlatformUpgrades()
    {
        MajorPlatformUpgrades.Clear();
        MajorPlatformUpgrades.Add("Multi-registry federation searches official, private, local dev, and game community registries with per-registry policy decisions.");
        MajorPlatformUpgrades.Add("Hosted build farm queues reproducible builds, signatures, SBOM output, scans, and provenance attestations before publish.");
        MajorPlatformUpgrades.Add("Publisher economy supports verified publishers, donations, paid listings, licensing, team ownership, collaborators, and revenue reports.");
        MajorPlatformUpgrades.Add("SAT dependency solver resolves versions, conflicts, optional dependencies, peer dependencies, game/runtime constraints, and lockfiles.");
        MajorPlatformUpgrades.Add("Cloud profiles sync installed mods, modpacks, load order, favorites, trust decisions, profiles, and account settings across devices.");
        MajorPlatformUpgrades.Add("Remote orchestration can push install commands to online gaming PCs, Steam Deck-style clients, home servers, and LAN-discovered desktops.");
        MajorPlatformUpgrades.Add("Compatibility lab runs adapter-based sandbox jobs, captures logs, detects crashes/freezes, and records compatibility results.");
        MajorPlatformUpgrades.Add("Workflow automation evaluates rules for game updates, pre-launch backups, crash diagnostics, and stable-ring update installs.");
        MajorPlatformUpgrades.Add("First-class modpacks now have maintainers, versions, changelogs, screenshots, lockfiles, compatibility matrices, one-click install, and rollback points.");
        MajorPlatformUpgrades.Add(new RuntimeObservabilityService().Summarize(new ModRuntimeMetric
        {
            ModId = "example-mod",
            LoadDuration = TimeSpan.FromMilliseconds(42),
            MemoryDeltaBytes = 1024 * 256,
            EventHandlersRegistered = 3,
            CommandsAdded = 2,
            ExceptionsThrown = 0,
            FpsImpact = 0.2,
            LastSuccessfulLoad = DateTimeOffset.UtcNow
        }));
        MajorPlatformUpgrades.Add("Trust engine combines signatures, publisher verification, scans, crash reports, permission risk, installs, update history, and user reports.");
        MajorPlatformUpgrades.Add("Policy-as-code stores enterprise/team rules for unsigned mods, allowed registries, blocked permissions, trust level, and blocked package IDs.");
        MajorPlatformUpgrades.Add("Extension marketplace publishes TheUnlocker adapters, themes, package format plugins, scanner plugins, marketplace panels, and workflow actions.");
        MajorPlatformUpgrades.Add("AI compatibility assistant inspects manifests, logs, crash stacks, dependency graphs, permissions, and targets to suggest fixes.");
        MajorPlatformUpgrades.Add("Desktop self-updater supports stable/beta/nightly channels, signed updates, changelog UI, release health checks, and rollback planning.");
    }

    private void ToggleUnsignedPolicy()
    {
        new TrustPolicyEditorService(Path.Combine(_contentRoot, "content-config.json")).ToggleUnsignedMods();
        RefreshCodeMods();
    }

    private void ResetPolicies()
    {
        ImportStatusMessage = new DesktopRecoveryCenterService(_contentRoot).ResetPolicies();
        RefreshCodeMods();
    }

    private void EnableSafeMode()
    {
        ImportStatusMessage = new DesktopRecoveryCenterService(_contentRoot).EnableSafeMode();
        RefreshCodeMods();
    }

    private void DisableLastChangedMods()
    {
        ImportStatusMessage = new DesktopRecoveryCenterService(_contentRoot).DisableLastChangedMods();
        RefreshCodeMods();
    }

    private void LinkDevMod()
    {
        var dialog = new OpenFolderDialog { Title = "Select a local mod project folder" };
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            var target = new LocalModDevelopmentService().LinkProject(dialog.FolderName, Path.Combine(_contentRoot, "Mods"));
            ImportStatusMessage = $"Development mod linked: {target}";
            RefreshCodeMods();
        }
        catch (Exception ex)
        {
            ImportStatusMessage = $"Development link failed: {ex.Message}";
        }
    }

    private async Task SignInAsync()
    {
        try
        {
            var auth = new AuthService();
            var result = await new DesktopAccountSyncService(new ApiClient(auth)).SignInAndSyncAsync(RegistryApiUrl, AccountEmail, AccountPassword);
            AccountPassword = "";
            AccountStatusMessage = result.Message;
        }
        catch (Exception ex)
        {
            AccountStatusMessage = $"Sign-in failed: {ex.Message}";
        }
    }

    private void SignOut()
    {
        AccountPassword = "";
        AccountStatusMessage = "Signed out locally.";
    }

    private void RegisterProtocol()
    {
        try
        {
            var output = Path.Combine(_contentRoot, "theunlocker-protocol.reg");
            Directory.CreateDirectory(_contentRoot);
            var executable = Environment.ProcessPath ?? "TheUnlocker.exe";
            File.WriteAllText(output, new ProtocolRegistration().CreateRegistryFileContent(executable));
            ImportStatusMessage = $"Protocol registration file written: {output}";
        }
        catch (Exception ex)
        {
            ImportStatusMessage = $"Protocol registration failed: {ex.Message}";
        }
    }

    private async Task ProcessInstallQueueAsync()
    {
        try
        {
            var service = new InstallQueueService();
            foreach (var item in InstallQueue.Where(item => item.Status.Equals("Queued", StringComparison.OrdinalIgnoreCase)).ToArray())
            {
                service.Enqueue(item.Source);
            }

            await service.ProcessAsync(new ModInstaller(Path.Combine(_contentRoot, "Mods"), Path.Combine(_contentRoot, "Quarantine")));
            InstallQueue.Clear();
            foreach (var item in service.History)
            {
                InstallQueue.Add(item);
            }
            ImportStatusMessage = "Install queue processed: download -> verify -> scan -> dependencies -> install -> rollback record.";
        }
        catch (Exception ex)
        {
            ImportStatusMessage = $"Install queue failed: {ex.Message}";
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
