using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using TheUnlocker.Configuration;
using TheUnlocker.Graph;
using TheUnlocker.Workspaces;

namespace TheUnlocker.Modding;

public sealed class ModLoader : IModLoader
{
    private static readonly Version RuntimeSdkVersion = new(1, 0, 0);
    private const int CrashDisableThreshold = 3;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private readonly string _modsDirectory;
    private readonly string _logPath;
    private readonly string _quarantineDirectory;
    private readonly Version _appVersion;
    private readonly LocalAppConfigStore _configStore;
    private readonly ModInstaller _installer;
    private readonly ModServiceRegistry _services;
    private readonly ModManifestValidator _validator = new();
    private readonly List<LoadedModInfo> _loadedMods = new();
    private readonly List<string> _logs = new();
    private readonly List<ModLogEntry> _logEntries = new();
    private readonly List<ModSettingInfo> _modSettings = new();
    private readonly List<ModLoadOrderInfo> _loadOrder = new();
    private readonly List<ModConflictInfo> _conflicts = new();
    private readonly List<ModHealthInfo> _health = new();
    private readonly List<ModUpdateInfo> _updates = new();
    private readonly List<ModRepositoryEntry> _marketplace = new();
    private readonly List<WeakReference> _unloadReferences = new();
    private readonly Dictionary<string, int> _eventHandlerCounts = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _commandCounts = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<TimeSpan>> _loadTimes = new(StringComparer.OrdinalIgnoreCase);
    private LocalAppConfig _lastConfig = new();

    public ModLoader(string modsDirectory, string configPath, string logsDirectory, Version appVersion)
    {
        _modsDirectory = modsDirectory;
        _appVersion = appVersion;
        _configStore = new LocalAppConfigStore(configPath);
        _quarantineDirectory = Path.Combine(Path.GetDirectoryName(modsDirectory) ?? modsDirectory, "Quarantine");
        _installer = new ModInstaller(modsDirectory, _quarantineDirectory);
        _services = new ModServiceRegistry(_configStore);
        _logPath = Path.Combine(logsDirectory, "mod-loader.log");
    }

    public IReadOnlyCollection<LoadedModInfo> LoadedMods => _loadedMods;

    public IReadOnlyCollection<string> Logs => _logs;

    public IReadOnlyCollection<ModLogEntry> LogEntries => _logEntries;

    public IReadOnlyCollection<ModSettingInfo> ModSettings => _modSettings;

    public IReadOnlyCollection<ModLoadOrderInfo> LoadOrder => _loadOrder;

    public IReadOnlyCollection<ModConflictInfo> Conflicts => _conflicts;

    public IReadOnlyCollection<ModHealthInfo> Health => _health;

    public IReadOnlyCollection<ModUpdateInfo> Updates => _updates;

    public IReadOnlyCollection<ModRepositoryEntry> Marketplace => _marketplace;

    public IReadOnlyCollection<ModRegistryEntry> RegistryEntries => new LocalPackageRegistry(Path.Combine(Path.GetDirectoryName(_modsDirectory) ?? _modsDirectory, "Registry")).Entries;

    public IReadOnlyCollection<string> Profiles => _lastConfig.Profiles.Keys.Append("Default").Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(profile => profile).ToArray();

    public string ActiveProfile => string.IsNullOrWhiteSpace(_lastConfig.ActiveProfile) ? "Default" : _lastConfig.ActiveProfile;

    public void LoadMods()
    {
        UnloadMods();
        _modSettings.Clear();
        Directory.CreateDirectory(_modsDirectory);
        Directory.CreateDirectory(_quarantineDirectory);
        Directory.CreateDirectory(Path.GetDirectoryName(_logPath)!);

        var config = _configStore.Load();
        _lastConfig = config;
        if (config.SafeMode)
        {
            AppendLog("system", "Warning", "SafeMode", "Safe mode is active. Code mods are not loaded.");
            return;
        }
        var discoveredMods = DiscoverMods();
        var orderedMods = OrderByDependencies(discoveredMods);
        AddLoadOrder(orderedMods);
        AddConflicts(discoveredMods.Values);
        RefreshUpdates(discoveredMods.Values);

        foreach (var mod in orderedMods)
        {
            LoadDiscoveredMod(mod, config, discoveredMods);
        }
    }

    public void SetModEnabled(string modId, bool enabled)
    {
        _configStore.Update(config =>
        {
            var enabledMods = GetEnabledSet(config);
            if (enabled)
            {
                enabledMods.Add(modId);
                config.EnabledSince[modId] = DateTimeOffset.Now;
            }
            else
            {
                enabledMods.Remove(modId);
                config.EnabledSince.Remove(modId);
            }
        });

        LoadMods();
    }

    public string InstallMod(string sourcePath)
    {
        var message = _installer.Install(sourcePath);
        AppendLog(message);
        LoadMods();
        return message;
    }

    public string InstallMarketplaceMod(string modId)
    {
        RefreshUpdates();
        var entry = _marketplace.FirstOrDefault(entry => entry.Id.Equals(modId, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Marketplace mod was not found: {modId}");

        var tempFile = Path.Combine(Path.GetTempPath(), $"{entry.Id}-{Guid.NewGuid():N}.zip");
        try
        {
            using var client = new HttpClient();
            File.WriteAllBytes(tempFile, client.GetByteArrayAsync(entry.DownloadUrl).GetAwaiter().GetResult());

            if (!string.IsNullOrWhiteSpace(entry.Sha256))
            {
                var actual = ComputeSha256(tempFile);
                if (!actual.Equals(entry.Sha256, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("Downloaded package hash did not match the repository index.");
                }
            }

            return InstallMod(tempFile);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    public string InstallModpack(string lockfileUrlOrPath)
    {
        var messages = new ModpackLockfileResolver()
            .InstallAsync(lockfileUrlOrPath, _installer)
            .GetAwaiter()
            .GetResult();
        LoadMods();
        return $"Installed modpack: {string.Join("; ", messages)}";
    }

    public string RollbackMod(string modId, string packagePath)
    {
        if (!File.Exists(packagePath))
        {
            throw new FileNotFoundException("Rollback package was not found.", packagePath);
        }

        var message = _installer.Install(packagePath);
        AppendLog($"[{modId}] Rollback installed from {packagePath}");
        LoadMods();
        return message;
    }

    public string ExportDependencyGraph(string outputPath)
    {
        var manifests = Directory.EnumerateFiles(_modsDirectory, "mod.json", SearchOption.AllDirectories)
            .Select(path => JsonSerializer.Deserialize<ModManifest>(File.ReadAllText(path), JsonOptions))
            .Where(manifest => manifest is not null)
            .Cast<ModManifest>()
            .ToArray();
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? ".");
        File.WriteAllText(outputPath, new DependencyGraphExporter().ToMermaid(manifests));
        return outputPath;
    }

    public string ExportDiagnostics(string outputDirectory)
    {
        return ModDiagnosticsExporter.Export(
            outputDirectory,
            _configStore.Load(),
            _logs,
            _health,
            Directory.EnumerateFiles(_modsDirectory, "mod.json", SearchOption.AllDirectories));
    }

    public string GenerateCompatibilityReport(string outputDirectory)
    {
        return ModReportGenerator.GenerateCompatibilityReport(
            outputDirectory,
            _appVersion,
            Directory.EnumerateFiles(_modsDirectory, "mod.json", SearchOption.AllDirectories));
    }

    public string GenerateModDocumentation(string modId, string outputDirectory)
    {
        var manifestPath = Directory.EnumerateFiles(_modsDirectory, "mod.json", SearchOption.AllDirectories)
            .FirstOrDefault(path =>
            {
                var manifest = JsonSerializer.Deserialize<ModManifest>(File.ReadAllText(path), JsonOptions);
                return manifest?.Id.Equals(modId, StringComparison.OrdinalIgnoreCase) == true;
            }) ?? throw new InvalidOperationException($"Mod was not found: {modId}");

        return ModReportGenerator.GenerateModDocumentation(manifestPath, outputDirectory);
    }

    public void SetModSetting(string modId, string key, string value)
    {
        _configStore.Update(config =>
        {
            if (!config.ModSettings.TryGetValue(modId, out var settings))
            {
                settings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                config.ModSettings[modId] = settings;
            }

            settings[key] = value;
        });

        LoadMods();
    }

    public void SetActiveProfile(string profileName)
    {
        _configStore.Update(config =>
        {
            config.ActiveProfile = string.IsNullOrWhiteSpace(profileName) ? "Default" : profileName;
            if (!config.ActiveProfile.Equals("Default", StringComparison.OrdinalIgnoreCase)
                && !config.Profiles.ContainsKey(config.ActiveProfile))
            {
                config.Profiles[config.ActiveProfile] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }
        });

        LoadMods();
    }

    public void SaveProfile(string profileName)
    {
        _configStore.Update(config =>
        {
            var name = string.IsNullOrWhiteSpace(profileName) ? ActiveProfile : profileName;
            config.Profiles[name] = config.EnabledMods.ToHashSet(StringComparer.OrdinalIgnoreCase);
            config.ActiveProfile = name;
        });

        LoadMods();
    }

    public void RefreshUpdates()
    {
        var discoveredMods = DiscoverMods();
        RefreshUpdates(discoveredMods.Values);
    }

    public void UnloadMods()
    {
        foreach (var mod in _loadedMods.Where(mod => mod.Instance is not null))
        {
            try
            {
                if (mod.Instance is IAsyncModLifecycle asyncLifecycle)
                {
                    asyncLifecycle.OnUnloadAsync(CancellationToken.None).GetAwaiter().GetResult();
                }

                mod.Instance!.OnUnload();
            }
            catch (Exception ex)
            {
                AppendLog($"[{mod.Id}] OnUnload failed: {ex}");
            }
        }

        foreach (var context in _loadedMods.Select(mod => mod.LoadContext).Where(context => context is not null))
        {
            _unloadReferences.Add(new WeakReference(context!));
            context!.Unload();
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var stillAlive = _unloadReferences.Count(reference => reference.IsAlive);
        if (_unloadReferences.Count > 0)
        {
            AppendLog($"[unload] {_unloadReferences.Count - stillAlive}/{_unloadReferences.Count} collectible load contexts released.");
        }

        _loadedMods.Clear();
        _modSettings.Clear();
        _loadOrder.Clear();
        _conflicts.Clear();
        _health.Clear();
    }

    private IReadOnlyDictionary<string, ModDiscoveryInfo> DiscoverMods()
    {
        var discovered = new Dictionary<string, ModDiscoveryInfo>(StringComparer.OrdinalIgnoreCase);

        foreach (var manifestPath in Directory.EnumerateFiles(_modsDirectory, "mod.json", SearchOption.AllDirectories))
        {
            try
            {
                var manifest = JsonSerializer.Deserialize<ModManifest>(File.ReadAllText(manifestPath), JsonOptions);
                if (manifest is null || string.IsNullOrWhiteSpace(manifest.Id))
                {
                    AddDiscoveryError(manifestPath, "Manifest is missing a required id.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(manifest.EntryDll))
                {
                    AddDiscoveryError(manifestPath, "Manifest is missing a required entryDll.");
                    continue;
                }

                var modDirectory = Path.GetDirectoryName(manifestPath) ?? _modsDirectory;
                var entryDllPath = Path.Combine(modDirectory, manifest.EntryDll);

                discovered[manifest.Id] = new ModDiscoveryInfo
                {
                    Manifest = manifest,
                    ManifestPath = manifestPath,
                    DirectoryPath = modDirectory,
                    EntryDllPath = entryDllPath
                };
            }
            catch (Exception ex)
            {
                AddDiscoveryError(manifestPath, $"Manifest could not be read: {ex.Message}");
            }
        }

        return discovered;
    }

    private IReadOnlyList<ModDiscoveryInfo> OrderByDependencies(IReadOnlyDictionary<string, ModDiscoveryInfo> discoveredMods)
    {
        var ordered = new List<ModDiscoveryInfo>();
        var temporary = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var permanent = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var mod in discoveredMods.Values.OrderBy(mod => mod.Manifest.Id))
        {
            Visit(mod);
        }

        return ordered;

        void Visit(ModDiscoveryInfo mod)
        {
            if (permanent.Contains(mod.Manifest.Id))
            {
                return;
            }

            if (!temporary.Add(mod.Manifest.Id))
            {
                AddSkipped(mod, ModLoadStatus.Error, "Dependency cycle detected.");
                return;
            }

            foreach (var dependencyId in mod.Manifest.DependsOn)
            {
                if (discoveredMods.TryGetValue(dependencyId, out var dependency))
                {
                    Visit(dependency);
                }
            }

            temporary.Remove(mod.Manifest.Id);
            permanent.Add(mod.Manifest.Id);

            if (!ordered.Any(existing => existing.Manifest.Id.Equals(mod.Manifest.Id, StringComparison.OrdinalIgnoreCase)))
            {
                ordered.Add(mod);
            }
        }
    }

    private void LoadDiscoveredMod(
        ModDiscoveryInfo mod,
        LocalAppConfig config,
        IReadOnlyDictionary<string, ModDiscoveryInfo> discoveredMods)
    {
        var manifest = mod.Manifest;
        var enabledMods = GetEnabledSet(config);
        var enabled = enabledMods.Contains(manifest.Id);
        var signatureStatus = GetSignatureStatus(mod, config);
        AddSettings(mod, config);

        var validation = _validator.Validate(manifest, mod.DirectoryPath, discoveredMods.Keys.ToArray());
        if (!validation.IsValid)
        {
            AddSkipped(mod, ModLoadStatus.Error, string.Join("; ", validation.Errors), signatureStatus, enabled);
            QuarantineInstalledMod(mod, "invalid-manifest");
            return;
        }

        if (!enabled)
        {
            AddSkipped(mod, ModLoadStatus.Disabled, "Installed but disabled.", signatureStatus, enabled);
            return;
        }

        var dependencyError = GetDependencyError(manifest, discoveredMods, config);
        if (dependencyError is not null)
        {
            AddSkipped(mod, ModLoadStatus.MissingDependency, dependencyError, signatureStatus, enabled);
            return;
        }

        var compatibilityError = GetCompatibilityError(manifest);
        if (compatibilityError is not null)
        {
            AddSkipped(mod, ModLoadStatus.Incompatible, compatibilityError, signatureStatus, enabled);
            return;
        }

        if (!File.Exists(mod.EntryDllPath))
        {
            AddSkipped(mod, ModLoadStatus.Error, $"Entry DLL is missing: {manifest.EntryDll}", signatureStatus, enabled);
            return;
        }

        if (signatureStatus == ModSignatureStatus.HashMismatch)
        {
            AddSkipped(mod, ModLoadStatus.Error, "Signature hash does not match the entry DLL.", signatureStatus, enabled);
            return;
        }

        var policyError = GetPolicyError(manifest, signatureStatus, config);
        if (policyError is not null)
        {
            AddSkipped(mod, ModLoadStatus.Error, policyError, signatureStatus, enabled);
            return;
        }

        if (config.UnsafeMods.Contains(manifest.Id))
        {
            AddSkipped(mod, ModLoadStatus.Error, "Automatically disabled after repeated crashes.", signatureStatus, enabled);
            return;
        }

        if (manifest.IsolationMode == ModIsolationMode.OutOfProcess)
        {
            AddSkipped(mod, ModLoadStatus.Disabled, "Out-of-process isolation is staged for IPC hosts and is not loaded in-process.", signatureStatus, enabled);
            return;
        }

        CreateAndLoadMod(mod, signatureStatus, config);
    }

    private string? GetDependencyError(
        ModManifest manifest,
        IReadOnlyDictionary<string, ModDiscoveryInfo> discoveredMods,
        LocalAppConfig config)
    {
        foreach (var dependencyId in manifest.DependsOn)
        {
            if (!discoveredMods.ContainsKey(dependencyId))
            {
                return $"Missing dependency: {dependencyId}";
            }

            if (!GetEnabledSet(config).Contains(dependencyId))
            {
                return $"Dependency is installed but disabled: {dependencyId}";
            }
        }

        foreach (var dependency in manifest.Dependencies)
        {
            if (string.IsNullOrWhiteSpace(dependency.Id))
            {
                continue;
            }

            if (!discoveredMods.TryGetValue(dependency.Id, out var installedDependency))
            {
                if (dependency.Optional)
                {
                    continue;
                }

                return $"Missing dependency: {dependency.Id}";
            }

            if (!dependency.Optional && !GetEnabledSet(config).Contains(dependency.Id))
            {
                return $"Dependency is installed but disabled: {dependency.Id}";
            }

            if (!IsVersionInRange(installedDependency.Manifest.Version, dependency.VersionRange))
            {
                return $"Dependency {dependency.Id} version {installedDependency.Manifest.Version} does not satisfy {dependency.VersionRange}";
            }
        }

        return null;
    }

    private static bool IsVersionInRange(string versionText, string range)
    {
        if (string.IsNullOrWhiteSpace(range) || !Version.TryParse(versionText, out var version))
        {
            return true;
        }

        foreach (var part in range.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (part.StartsWith(">=", StringComparison.Ordinal) && Version.TryParse(part[2..], out var minimum) && version < minimum)
            {
                return false;
            }

            if (part.StartsWith("<=", StringComparison.Ordinal) && Version.TryParse(part[2..], out var maximumInclusive) && version > maximumInclusive)
            {
                return false;
            }

            if (part.StartsWith("<", StringComparison.Ordinal) && Version.TryParse(part[1..], out var maximum) && version >= maximum)
            {
                return false;
            }

            if (part.StartsWith(">", StringComparison.Ordinal) && Version.TryParse(part[1..], out var exclusiveMinimum) && version <= exclusiveMinimum)
            {
                return false;
            }
        }

        return true;
    }

    private string? GetCompatibilityError(ModManifest manifest)
    {
        if (Version.TryParse(manifest.MinimumAppVersion, out var minimumAppVersion)
            && _appVersion < minimumAppVersion)
        {
            return $"Requires app version {minimumAppVersion} or newer.";
        }

        if (Version.TryParse(manifest.MinimumFrameworkVersion, out var minimumFrameworkVersion)
            && Environment.Version < minimumFrameworkVersion)
        {
            return $"Requires .NET runtime {minimumFrameworkVersion} or newer.";
        }

        if (Version.TryParse(manifest.SdkVersion, out var sdkVersion)
            && sdkVersion.Major != RuntimeSdkVersion.Major)
        {
            return $"Requires incompatible mod SDK version {sdkVersion}. Runtime SDK is {RuntimeSdkVersion}.";
        }

        return null;
    }

    private static string? GetPolicyError(ModManifest manifest, ModSignatureStatus signatureStatus, LocalAppConfig config)
    {
        if (config.Policy.BlockedMods.Contains(manifest.Id))
        {
            return "Blocked by local mod policy.";
        }

        if (config.Policy.AllowedPublishers.Count > 0
            && (string.IsNullOrWhiteSpace(manifest.PublisherId) || !config.Policy.AllowedPublishers.Contains(manifest.PublisherId)))
        {
            return "Publisher is not allowed by local mod policy.";
        }

        if (!config.Policy.AllowUnsignedMods && signatureStatus == ModSignatureStatus.Unsigned)
        {
            return "Unsigned mods are disabled by local mod policy.";
        }

        return null;
    }

    private ModSignatureStatus GetSignatureStatus(ModDiscoveryInfo mod, LocalAppConfig config)
    {
        if (string.IsNullOrWhiteSpace(mod.Manifest.Signature?.Sha256))
        {
            return ModSignatureStatus.Unsigned;
        }

        if (!File.Exists(mod.EntryDllPath))
        {
            return ModSignatureStatus.HashMismatch;
        }

        var actualHash = ComputeSha256(mod.EntryDllPath);
        if (!actualHash.Equals(mod.Manifest.Signature.Sha256, StringComparison.OrdinalIgnoreCase))
        {
            return ModSignatureStatus.HashMismatch;
        }

        if (!VerifyPublisherSignature(mod, config))
        {
            return ModSignatureStatus.HashMismatch;
        }

        return !string.IsNullOrWhiteSpace(mod.Manifest.PublisherId)
            && (config.TrustedPublishers.Contains(mod.Manifest.PublisherId)
                || config.TrustedPublisherKeys.ContainsKey(mod.Manifest.PublisherId))
            ? ModSignatureStatus.Verified
            : ModSignatureStatus.UntrustedPublisher;
    }

    private static bool VerifyPublisherSignature(ModDiscoveryInfo mod, LocalAppConfig config)
    {
        if (string.IsNullOrWhiteSpace(mod.Manifest.Signature?.RsaSha256))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(mod.Manifest.PublisherId)
            || !config.TrustedPublisherKeys.TryGetValue(mod.Manifest.PublisherId, out var publicKeyPem))
        {
            return false;
        }

        try
        {
            using var rsa = RSA.Create();
            rsa.ImportFromPem(publicKeyPem);
            var signedPayload = $"{mod.Manifest.Id}|{mod.Manifest.Version}|{mod.Manifest.Signature.Sha256}";
            var signatureBytes = Convert.FromBase64String(mod.Manifest.Signature.RsaSha256);
            return rsa.VerifyData(
                System.Text.Encoding.UTF8.GetBytes(signedPayload),
                signatureBytes,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);
        }
        catch
        {
            return false;
        }
    }

    private static string ComputeSha256(string path)
    {
        using var stream = File.OpenRead(path);
        var hash = SHA256.HashData(stream);
        return Convert.ToHexString(hash);
    }

    private void CreateAndLoadMod(ModDiscoveryInfo discovered, ModSignatureStatus signatureStatus, LocalAppConfig config)
    {
        var manifest = discovered.Manifest;
        var loadContext = new ModAssemblyLoadContext(discovered.EntryDllPath);
        var stopwatch = Stopwatch.StartNew();
        var memoryBefore = GC.GetTotalMemory(forceFullCollection: false);

        try
        {
            var assembly = loadContext.LoadFromAssemblyPath(Path.GetFullPath(discovered.EntryDllPath));
            var modType = GetModTypes(assembly).FirstOrDefault();

            if (modType is null)
            {
                loadContext.Unload();
                AddSkipped(discovered, ModLoadStatus.Error, "No public, non-abstract IMod implementation was found.", signatureStatus, true);
                return;
            }

            if (Activator.CreateInstance(modType) is not IMod modInstance)
            {
                loadContext.Unload();
                AddSkipped(discovered, ModLoadStatus.Error, "The IMod type could not be created.", signatureStatus, true);
                return;
            }

            var permissions = manifest.Permissions.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var token = new CapabilityToken(manifest.Id, permissions);
            var eventBus = new ModEventBus(permissions, () => Increment(_eventHandlerCounts, manifest.Id));
            var context = new ModContext(
                _modsDirectory,
                discovered.DirectoryPath,
                permissions,
                token,
                _services.CreateMenuService(token),
                _services.CreateAssetRegistry(token),
                _services.CreateNotificationService(token),
                _services.CreateSettingsService(token),
                eventBus,
                _services.CreateNavigationService(token),
                _services.CreateAssetImporterRegistry(token),
                _services.CreateThemeRegistry(token),
                _services.CreateCommandPalette(token, () => Increment(_commandCounts, manifest.Id)),
                _services.CreateToolPanelRegistry(token),
                AppendLog);

            foreach (var schema in manifest.EventSchemas)
            {
                eventBus.RegisterSchema(schema, manifest.SdkVersion is { Length: > 0 } && Version.TryParse(manifest.SdkVersion, out var schemaVersion) ? schemaVersion : new Version(1, 0, 0));
            }

            RunMigrationIfNeeded(modInstance, manifest, context, config);

            try
            {
                if (modInstance is IModLifecycle lifecycle)
                {
                    lifecycle.OnPreLoad(context);
                }

                if (modInstance is IAsyncModLifecycle asyncLifecycle)
                {
                    asyncLifecycle.OnPreLoadAsync(context, CancellationToken.None).GetAwaiter().GetResult();
                    asyncLifecycle.OnLoadAsync(context, CancellationToken.None).GetAwaiter().GetResult();
                }

                modInstance.OnLoad(context);

                if (modInstance is IModLifecycle readyLifecycle)
                {
                    readyLifecycle.OnAppReady(context);
                }

                if (modInstance is IAsyncModLifecycle readyAsyncLifecycle)
                {
                    readyAsyncLifecycle.OnAppReadyAsync(context, CancellationToken.None).GetAwaiter().GetResult();
                }
            }
            catch (Exception ex)
            {
                loadContext.Unload();
                AddSkipped(discovered, ModLoadStatus.Error, $"OnLoad crashed: {ex.Message}", signatureStatus, true);
                AddHealth(manifest.Id, "Error", stopwatch.Elapsed, memoryBefore, ex.Message, manifest.Permissions, config);
                RegisterCrash(manifest.Id);
                AppendLog($"[{manifest.Id}] OnLoad crashed: {ex}");
                return;
            }

            stopwatch.Stop();
            AddLoadTime(manifest.Id, stopwatch.Elapsed);
            AddHealth(manifest.Id, "Loaded", stopwatch.Elapsed, memoryBefore, "", manifest.Permissions, config);
            _configStore.Update(nextConfig =>
            {
                nextConfig.LastLoadedVersions[manifest.Id] = manifest.Version;
                nextConfig.CrashCounts.Remove(manifest.Id);
                nextConfig.UnsafeMods.Remove(manifest.Id);
            });

            _loadedMods.Add(new LoadedModInfo
            {
                Id = manifest.Id,
                Name = string.IsNullOrWhiteSpace(manifest.Name) ? modInstance.Name : manifest.Name,
                Version = manifest.Version,
                Author = manifest.Author,
                Description = manifest.Description,
                AssemblyPath = discovered.EntryDllPath,
                Status = ModLoadStatus.Loaded,
                SignatureStatus = signatureStatus,
                IsEnabled = true,
                Dependencies = manifest.DependsOn,
                Permissions = manifest.Permissions,
                Targets = manifest.Targets,
                Settings = manifest.Settings,
                Message = "Loaded successfully.",
                Instance = modInstance,
                LoadContext = loadContext
            });
        }
        catch (Exception ex)
        {
            loadContext.Unload();
            AddSkipped(discovered, ModLoadStatus.Error, ex.Message, signatureStatus, true);
            AddHealth(manifest.Id, "Error", stopwatch.Elapsed, memoryBefore, ex.Message, manifest.Permissions, _lastConfig);
            RegisterCrash(manifest.Id);
            AppendLog($"[{manifest.Id}] Load failed: {ex}");
        }
    }

    private static IEnumerable<Type> GetModTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes()
                .Where(IsModType);
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types
                .Where(type => type is not null)
                .Cast<Type>()
                .Where(IsModType);
        }
    }

    private static bool IsModType(Type type)
    {
        return typeof(IMod).IsAssignableFrom(type)
            && type is { IsAbstract: false, IsInterface: false }
            && type.GetConstructor(Type.EmptyTypes) is not null;
    }

    private void RunMigrationIfNeeded(IMod modInstance, ModManifest manifest, ModContext context, LocalAppConfig config)
    {
        if (modInstance is not IModMigration migration)
        {
            return;
        }

        config.LastLoadedVersions.TryGetValue(manifest.Id, out var previousVersionText);
        if (!Version.TryParse(previousVersionText, out var previousVersion))
        {
            previousVersion = new Version(0, 0, 0);
        }

        if (!Version.TryParse(manifest.Version, out var currentVersion) || previousVersion >= currentVersion)
        {
            return;
        }

        migration.Migrate(new ModMigrationContext(
            manifest.Id,
            previousVersion,
            currentVersion,
            context.Settings,
            AppendLog));

        _configStore.Update(nextConfig => nextConfig.LastLoadedVersions[manifest.Id] = manifest.Version);
    }

    private void RegisterCrash(string modId)
    {
        _configStore.Update(config =>
        {
            config.CrashCounts.TryGetValue(modId, out var count);
            count++;
            config.CrashCounts[modId] = count;

            if (count >= CrashDisableThreshold)
            {
                config.UnsafeMods.Add(modId);
                GetEnabledSet(config).Remove(modId);
                AppendLog($"[{modId}] Disabled after {count} crashes.");
            }
        });
    }

    private void AddDiscoveryError(string path, string message)
    {
        _loadedMods.Add(new LoadedModInfo
        {
            Id = Path.GetFileNameWithoutExtension(Path.GetDirectoryName(path) ?? path),
            Name = Path.GetFileName(path),
            Version = "n/a",
            AssemblyPath = path,
            Status = ModLoadStatus.Error,
            Message = message
        });

        AppendLog($"[discovery] {path}: {message}");
    }

    private void AddSkipped(
        ModDiscoveryInfo mod,
        ModLoadStatus status,
        string message,
        ModSignatureStatus signatureStatus = ModSignatureStatus.Unsigned,
        bool enabled = false)
    {
        _loadedMods.Add(new LoadedModInfo
        {
            Id = mod.Manifest.Id,
            Name = string.IsNullOrWhiteSpace(mod.Manifest.Name) ? mod.Manifest.Id : mod.Manifest.Name,
            Version = mod.Manifest.Version,
            Author = mod.Manifest.Author,
            Description = mod.Manifest.Description,
            AssemblyPath = mod.EntryDllPath,
            Status = status,
            SignatureStatus = signatureStatus,
            IsEnabled = enabled,
            Dependencies = mod.Manifest.DependsOn,
            Permissions = mod.Manifest.Permissions,
            Targets = mod.Manifest.Targets,
            Settings = mod.Manifest.Settings,
            Message = message
        });

        AppendLog($"[{mod.Manifest.Id}] {status}: {message}");
        AddHealth(mod.Manifest.Id, status.ToString(), TimeSpan.Zero, GC.GetTotalMemory(forceFullCollection: false), message, mod.Manifest.Permissions, _lastConfig);
    }

    private void AddSettings(ModDiscoveryInfo mod, LocalAppConfig config)
    {
        if (mod.Manifest.Settings.Count == 0)
        {
            return;
        }

        config.ModSettings.TryGetValue(mod.Manifest.Id, out var savedSettings);

        foreach (var setting in mod.Manifest.Settings)
        {
            var value = savedSettings is not null && savedSettings.TryGetValue(setting.Key, out var savedValue)
                ? savedValue
                : setting.Value.DefaultValue;

            _modSettings.Add(new ModSettingInfo
            {
                ModId = mod.Manifest.Id,
                Key = setting.Key,
                Label = string.IsNullOrWhiteSpace(setting.Value.Label) ? setting.Key : setting.Value.Label,
                Type = setting.Value.Type,
                Value = value,
                Options = setting.Value.Options
            });
        }
    }

    private HashSet<string> GetEnabledSet(LocalAppConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.ActiveProfile)
            || config.ActiveProfile.Equals("Default", StringComparison.OrdinalIgnoreCase))
        {
            return config.EnabledMods;
        }

        if (!config.Profiles.TryGetValue(config.ActiveProfile, out var profile))
        {
            profile = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            config.Profiles[config.ActiveProfile] = profile;
        }

        return profile;
    }

    private void AddLoadOrder(IReadOnlyList<ModDiscoveryInfo> orderedMods)
    {
        _loadOrder.Clear();
        for (var index = 0; index < orderedMods.Count; index++)
        {
            var mod = orderedMods[index];
            var reason = mod.Manifest.DependsOn.Length == 0
                ? "No dependencies."
                : $"After dependencies: {string.Join(", ", mod.Manifest.DependsOn)}";

            _loadOrder.Add(new ModLoadOrderInfo
            {
                Order = index + 1,
                ModId = mod.Manifest.Id,
                Reason = reason
            });
        }
    }

    private void AddConflicts(IEnumerable<ModDiscoveryInfo> mods)
    {
        _conflicts.Clear();

        var conflicts = mods
            .SelectMany(mod => mod.Manifest.Targets.Select(target => new { target, mod.Manifest.Id }))
            .GroupBy(item => item.target, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Select(item => item.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count() > 1);

        foreach (var conflict in conflicts)
        {
            _conflicts.Add(new ModConflictInfo
            {
                Target = conflict.Key,
                ModIds = string.Join(", ", conflict.Select(item => item.Id).Distinct(StringComparer.OrdinalIgnoreCase))
            });
        }
    }

    private void RefreshUpdates(IEnumerable<ModDiscoveryInfo> discoveredMods)
    {
        _updates.Clear();
        _marketplace.Clear();
        var config = _configStore.Load();
        if (string.IsNullOrWhiteSpace(config.RepositoryIndexPath))
        {
            return;
        }

        try
        {
            var json = Uri.TryCreate(config.RepositoryIndexPath, UriKind.Absolute, out var uri)
                && uri.Scheme.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                    ? new HttpClient().GetStringAsync(uri).GetAwaiter().GetResult()
                    : File.Exists(config.RepositoryIndexPath)
                        ? File.ReadAllText(config.RepositoryIndexPath)
                        : "";

            if (string.IsNullOrWhiteSpace(json))
            {
                return;
            }

            var index = JsonSerializer.Deserialize<ModRepositoryIndex>(json, JsonOptions);
            if (index is null)
            {
                return;
            }

            _marketplace.AddRange(index.Mods);

            foreach (var installed in discoveredMods)
            {
                var available = index.Mods.FirstOrDefault(entry => entry.Id.Equals(installed.Manifest.Id, StringComparison.OrdinalIgnoreCase));
            if (available is null)
            {
                continue;
            }

                if (Version.TryParse(installed.Manifest.Version, out var installedVersion)
                    && Version.TryParse(available.Version, out var availableVersion)
                    && availableVersion > installedVersion)
                {
                    _updates.Add(new ModUpdateInfo
                    {
                        ModId = installed.Manifest.Id,
                        InstalledVersion = installed.Manifest.Version,
                        AvailableVersion = available.Version,
                        DownloadUrl = available.DownloadUrl,
                        Changelog = available.Changelog,
                        NewPermissions = string.Join(", ", available.Permissions.Except(installed.Manifest.Permissions, StringComparer.OrdinalIgnoreCase))
                    });
                }
            }
        }
        catch (Exception ex)
        {
            AppendLog($"[repository] Update detection failed: {ex.Message}");
        }
    }

    private void AddHealth(
        string modId,
        string status,
        TimeSpan loadTime,
        long memoryBefore,
        string lastError,
        IReadOnlyCollection<string> servicesUsed,
        LocalAppConfig config)
    {
        var memoryAfter = GC.GetTotalMemory(forceFullCollection: false);
        var enabledDuration = config.EnabledSince.TryGetValue(modId, out var enabledSince)
            ? (DateTimeOffset.Now - enabledSince).ToString(@"d\.hh\:mm\:ss")
            : "";

        _health.RemoveAll(item => item.ModId.Equals(modId, StringComparison.OrdinalIgnoreCase));
        _health.Add(new ModHealthInfo
        {
            ModId = modId,
            Status = status,
            LoadTime = loadTime,
            MemoryDeltaBytes = memoryAfter - memoryBefore,
            LastError = lastError,
            ServicesUsed = servicesUsed.Count == 0 ? "none" : string.Join(", ", servicesUsed),
            EnabledDuration = enabledDuration,
            EventHandlersRegistered = _eventHandlerCounts.GetValueOrDefault(modId),
            CommandsAdded = _commandCounts.GetValueOrDefault(modId),
            ExceptionsThrown = config.CrashCounts.GetValueOrDefault(modId),
            LastSuccessfulLoad = config.LastLoadedVersions.ContainsKey(modId) ? DateTimeOffset.Now.ToString("O") : "",
            AverageLoadTime = GetAverageLoadTime(modId)
        });
    }

    private static void Increment(Dictionary<string, int> values, string modId)
    {
        values.TryGetValue(modId, out var current);
        values[modId] = current + 1;
    }

    private void AddLoadTime(string modId, TimeSpan elapsed)
    {
        if (!_loadTimes.TryGetValue(modId, out var values))
        {
            values = new List<TimeSpan>();
            _loadTimes[modId] = values;
        }

        values.Add(elapsed);
    }

    private string GetAverageLoadTime(string modId)
    {
        return _loadTimes.TryGetValue(modId, out var values) && values.Count > 0
            ? TimeSpan.FromMilliseconds(values.Average(value => value.TotalMilliseconds)).ToString()
            : "";
    }

    private void QuarantineInstalledMod(ModDiscoveryInfo mod, string reason)
    {
        try
        {
            var target = Path.Combine(_quarantineDirectory, $"{mod.Manifest.Id}-{reason}-{Guid.NewGuid():N}");
            if (Directory.Exists(target))
            {
                Directory.Delete(target, recursive: true);
            }

            Directory.CreateDirectory(_quarantineDirectory);
            Directory.Move(mod.DirectoryPath, target);
            AppendLog($"[{mod.Manifest.Id}] Moved to quarantine: {target}");
        }
        catch (Exception ex)
        {
            AppendLog($"[{mod.Manifest.Id}] Quarantine failed: {ex.Message}");
        }
    }

    private void AppendLog(string message)
    {
        AppendLog("system", "Info", "Runtime", message);
    }

    private void AppendLog(string modId, string severity, string eventType, string message)
    {
        var line = $"{DateTimeOffset.Now:O} {message}";
        _logs.Add(line);
        _logEntries.Add(new ModLogEntry
        {
            ModId = modId,
            Severity = severity,
            EventType = eventType,
            Message = message,
            Timestamp = DateTimeOffset.Now
        });

        Directory.CreateDirectory(Path.GetDirectoryName(_logPath)!);
        File.AppendAllLines(_logPath, [line]);
        Debug.WriteLine(line);
    }
}
