using System.Text.Json;
using TheUnlocker.Configuration;
using TheUnlocker.Desktop;
using TheUnlocker.Modding;
using TheUnlocker.PackageManager;
using TheUnlocker.Protocol;
using TheUnlocker.Workspaces;
using Xunit;

namespace TheUnlocker.Tests;

public sealed class ModdingRuntimeTests
{
    [Fact]
    [Trait("Category", "ManifestValidation")]
    public void ManifestValidationCatchesRequiredFieldsAndBadPermissions()
    {
        var directory = CreateTempDirectory();
        var validator = new ModManifestValidator();
        var manifest = new ModManifest
        {
            Id = "",
            Name = "",
            Version = "not-a-version",
            EntryDll = "Missing.dll",
            Permissions = ["BadPermission"]
        };

        var result = validator.Validate(manifest, directory, []);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("id", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Errors, error => error.Contains("Unknown permission", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    [Trait("Category", "DependencyResolution")]
    public void DependencyCycleValidationReportsCycles()
    {
        var directory = CreateTempDirectory();
        File.WriteAllText(Path.Combine(directory, "A.dll"), "");
        File.WriteAllText(Path.Combine(directory, "B.dll"), "");

        var mods = new[]
        {
            new ModDiscoveryInfo
            {
                DirectoryPath = directory,
                EntryDllPath = Path.Combine(directory, "A.dll"),
                ManifestPath = Path.Combine(directory, "a.json"),
                Manifest = new ModManifest { Id = "a", Name = "A", Version = "1.0.0", EntryDll = "A.dll", DependsOn = ["b"] }
            },
            new ModDiscoveryInfo
            {
                DirectoryPath = directory,
                EntryDllPath = Path.Combine(directory, "B.dll"),
                ManifestPath = Path.Combine(directory, "b.json"),
                Manifest = new ModManifest { Id = "b", Name = "B", Version = "1.0.0", EntryDll = "B.dll", DependsOn = ["a"] }
            }
        };

        var result = new ModManifestValidator().ValidateAll(mods);

        Assert.Contains(result.Errors, error => error.Contains("cycle", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    [Trait("Category", "Config")]
    public void ConfigPersistenceRoundTripsEnabledMods()
    {
        var path = Path.Combine(CreateTempDirectory(), "content-config.json");
        var store = new LocalAppConfigStore(path);

        store.Update(config => config.EnabledMods.Add("hello-world"));
        var loaded = store.Load();

        Assert.Contains("hello-world", loaded.EnabledMods);
    }

    [Fact]
    [Trait("Category", "DependencyResolution")]
    public void LoaderReportsMissingDependencyBeforeLoading()
    {
        var root = CreateTempDirectory();
        var mods = Path.Combine(root, "Mods");
        var mod = Path.Combine(mods, "dependent");
        Directory.CreateDirectory(mod);
        File.WriteAllText(Path.Combine(mod, "Dependent.dll"), "");

        File.WriteAllText(Path.Combine(mod, "mod.json"), JsonSerializer.Serialize(new ModManifest
        {
            Id = "dependent",
            Name = "Dependent",
            Version = "1.0.0",
            EntryDll = "Dependent.dll",
            DependsOn = ["missing-core"]
        }));

        var configPath = Path.Combine(root, "content-config.json");
        new LocalAppConfigStore(configPath).Update(config => config.EnabledMods.Add("dependent"));

        var loader = new ModLoader(mods, configPath, Path.Combine(root, "Logs"), new Version(1, 0, 0));
        loader.LoadMods();

        Assert.Contains(loader.LoadedMods, mod => mod.Id == "dependent" && mod.Status == ModLoadStatus.MissingDependency);
    }

    [Fact]
    [Trait("Category", "Import")]
    public void DllImportCreatesManifest()
    {
        var root = CreateTempDirectory();
        var dll = Path.Combine(root, "ImportedMod.dll");
        File.WriteAllText(dll, "");

        var installer = new ModInstaller(Path.Combine(root, "Mods"), Path.Combine(root, "Quarantine"));
        installer.Install(dll);

        Assert.True(File.Exists(Path.Combine(root, "Mods", "ImportedMod", "mod.json")));
    }

    [Fact]
    [Trait("Category", "Policy")]
    public void PolicyBlocksConfiguredMod()
    {
        var root = CreateTempDirectory();
        var mods = Path.Combine(root, "Mods");
        var mod = Path.Combine(mods, "blocked");
        Directory.CreateDirectory(mod);
        File.WriteAllText(Path.Combine(mod, "Blocked.dll"), "");
        File.WriteAllText(Path.Combine(mod, "mod.json"), JsonSerializer.Serialize(new ModManifest
        {
            Id = "blocked",
            Name = "Blocked",
            Version = "1.0.0",
            EntryDll = "Blocked.dll"
        }));

        var configPath = Path.Combine(root, "content-config.json");
        new LocalAppConfigStore(configPath).Update(config =>
        {
            config.EnabledMods.Add("blocked");
            config.Policy.BlockedMods.Add("blocked");
        });

        var loader = new ModLoader(mods, configPath, Path.Combine(root, "Logs"), new Version(1, 0, 0));
        loader.LoadMods();

        Assert.Contains(loader.LoadedMods, mod => mod.Id == "blocked" && mod.Message?.Contains("Blocked", StringComparison.OrdinalIgnoreCase) == true);
    }

    [Fact]
    [Trait("Category", "DependencyResolution")]
    public void OptionalDependencyDoesNotBlockLoadingChecks()
    {
        var manifest = new ModManifest
        {
            Id = "optional",
            Name = "Optional",
            Version = "1.0.0",
            EntryDll = "Optional.dll",
            Dependencies = [new ModDependency { Id = "missing", Optional = true, VersionRange = ">=1.0.0 <2.0.0" }]
        };

        var directory = CreateTempDirectory();
        File.WriteAllText(Path.Combine(directory, "Optional.dll"), "");
        var result = new ModManifestValidator().Validate(manifest, directory, [manifest.Id]);

        Assert.True(result.IsValid);
    }

    [Fact]
    [Trait("Category", "SafeMode")]
    public void SafeModeSkipsCodeModLoading()
    {
        var root = CreateTempDirectory();
        var configPath = Path.Combine(root, "content-config.json");
        new LocalAppConfigStore(configPath).Update(config => config.SafeMode = true);

        var loader = new ModLoader(Path.Combine(root, "Mods"), configPath, Path.Combine(root, "Logs"), new Version(1, 0, 0));
        loader.LoadMods();

        Assert.Contains(loader.LogEntries, entry => entry.EventType == "SafeMode");
    }

    [Fact]
    [Trait("Category", "PackageManager")]
    public void VersionComparerSupportsRanges()
    {
        Assert.True(VersionComparer.Satisfies("1.5.0", ">=1.2.0 <2.0.0"));
        Assert.False(VersionComparer.Satisfies("2.0.0", ">=1.2.0 <2.0.0"));
    }

    [Fact]
    [Trait("Category", "PackageManager")]
    public void ResolverReportsMissingRequiredDependencies()
    {
        var problems = new Resolver().GetProblems([
            new ModManifest
            {
                Id = "feature",
                Name = "Feature",
                Version = "1.0.0",
                EntryDll = "Feature.dll",
                Dependencies = [new ModDependency { Id = "core", VersionRange = ">=1.0.0", Optional = false }]
            }
        ]);

        Assert.Contains(problems, problem => problem.Contains("core", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    [Trait("Category", "PackageManager")]
    public void SatSolverSelectsCompatibleDependencyVersionsAndLockfile()
    {
        var result = new SatDependencySolver().Solve(new DependencySolveRequest
        {
            RequestedModIds = ["feature"],
            GameVersion = "1.5.0",
            SdkVersion = "1.0.0",
            Candidates =
            [
                new PackageCandidate
                {
                    Id = "feature",
                    Version = "1.0.0",
                    Dependencies = [new ModDependency { Id = "core", VersionRange = ">=1.2.0 <2.0.0" }]
                },
                new PackageCandidate { Id = "core", Version = "1.0.0" },
                new PackageCandidate { Id = "core", Version = "1.4.0" }
            ]
        });

        Assert.True(result.Success);
        Assert.Equal("1.4.0", result.Lockfile["core"]);
    }

    [Fact]
    [Trait("Category", "Policy")]
    public void PolicyAsCodeBlocksUnsafePermission()
    {
        var problems = new PolicyAsCodeService().Evaluate(
            new PolicyAsCodeDocument
            {
                AllowedRegistries = ["https://registry.example"],
                BlockedPermissions = ["NetworkAccess"]
            },
            "https://registry.example",
            ["NetworkAccess"],
            "TrustedPublisher",
            "network-mod");

        Assert.Contains(problems, problem => problem.Contains("NetworkAccess", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    [Trait("Category", "Protocol")]
    public void ProtocolRegistrationParsesInstallUri()
    {
        var modId = new ProtocolRegistration().TryParseInstallUri("theunlocker://install/better-graphics");

        Assert.Equal("better-graphics", modId);
    }

    [Fact]
    [Trait("Category", "Workspace")]
    public void WorkspaceServiceCreatesLockFileWithHashes()
    {
        var root = CreateTempDirectory();
        var package = Path.Combine(root, "hello-world.zip");
        File.WriteAllText(package, "package");

        var lockFile = new WorkspaceService().CreateLockFile([package]);

        Assert.Single(lockFile.Mods);
        Assert.False(string.IsNullOrWhiteSpace(lockFile.Mods[0].Sha256));
    }

    [Fact]
    [Trait("Category", "DesktopUpdate")]
    public void DesktopUpdaterSelectsHealthySignedReleaseAndHonorsPolicy()
    {
        var updater = new DesktopSelfUpdater();
        var release = new DesktopReleaseInfo
        {
            Version = "1.1.2",
            Channel = ReleaseChannel.Stable,
            SignatureUrl = "https://updates.example/theunlocker.sig",
            HealthCheckPassed = true
        };

        var selected = updater.SelectUpdate([release], ReleaseChannel.Stable, new Version(1, 0, 0));

        Assert.Equal("1.1.2", selected?.Version);
        Assert.True(updater.IsAllowedByPolicy(release, requireSignedUpdates: true, allowPrerelease: false));
        Assert.Contains("rollback", updater.CreateRollbackPlan(release), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Category", "DeviceFleet")]
    public void DeviceFleetSnapshotTracksCommandReadiness()
    {
        var snapshot = new TheUnlocker.Platform.DeviceFleetSnapshot(
            "demo-user",
            OnlineDevices: 1,
            PendingCommands: 1,
            DateTimeOffset.UnixEpoch,
            "fleet-key",
            [
                new TheUnlocker.Platform.FleetDeviceSnapshot(
                    "desktop-main",
                    "Main gaming PC",
                    "WindowsDesktop",
                    "online",
                    DateTimeOffset.UnixEpoch,
                    "Vanilla+",
                    42,
                    "TrustedDevice",
                    true,
                    [new TheUnlocker.Platform.FleetCommand("cmd-install", "install", "queued", "better-ui@1.4.0")])
            ]);

        Assert.Equal(1, snapshot.OnlineDevices);
        Assert.True(snapshot.Devices[0].CanReceiveCommands);
        Assert.Contains(snapshot.Devices[0].PendingCommands, command => command.Target.Contains("better-ui", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    [Trait("Category", "CompatibilityLab")]
    public void CompatibilityLabSnapshotCapturesFailedSandboxRecommendation()
    {
        var snapshot = new TheUnlocker.Platform.CompatibilityLabSnapshot(
            1,
            2,
            1,
            "91.4%",
            1,
            DateTimeOffset.UnixEpoch,
            new TheUnlocker.Platform.CompatibilityLabQueueSnapshot(Queued: 2, Running: 1, Failed: 1, DeadLetter: 0, AverageSeconds: 142),
            [
                new TheUnlocker.Platform.CompatibilityLabAdapterSnapshot(
                    "unity",
                    "Unity Adapter",
                    "healthy",
                    ["2022 LTS"],
                    DateTimeOffset.UnixEpoch)
            ],
            [
                new TheUnlocker.Platform.CompatibilityLabJobSnapshot(
                    "lab-mc-experimental-physics",
                    "experimental-physics-pack",
                    "minecraft",
                    "minecraft",
                    "completed",
                    "failed",
                    52,
                    DateTimeOffset.UnixEpoch,
                    ["Launch crash reproduced", "Bridge patch available"],
                    "NullReference: legacy-save-tools SavePatch.Apply",
                    "Recommend bridge-ui-save patch or disable legacy-save-tools.")
            ]);

        Assert.Equal(1, snapshot.FailedJobs);
        Assert.Contains(snapshot.Jobs, job => job.Result == "failed" && job.Recommendation.Contains("bridge", StringComparison.OrdinalIgnoreCase));
        Assert.Contains("2022 LTS", snapshot.Adapters[0].SupportedVersions);
    }

    [Fact]
    [Trait("Category", "BuildFarm")]
    public void BuildFarmSnapshotCapturesProvenanceAndBlockedScanState()
    {
        var snapshot = new TheUnlocker.Platform.BuildFarmSnapshot(
            1,
            2,
            38,
            1,
            "3m 42s",
            DateTimeOffset.UnixEpoch,
            [
                new TheUnlocker.Platform.BuildFarmWorkerSnapshot(
                    "worker-linux-repro-02",
                    "linux-reproducible",
                    "healthy",
                    "build-shared-ui-core-2-1-0",
                    ["go", "rust", "cyclonedx", "clamav"],
                    DateTimeOffset.UnixEpoch)
            ],
            [
                new TheUnlocker.Platform.BuildFarmJobSnapshot(
                    "build-debug-tools-0-9-0",
                    "debug-tools",
                    "0.9.0",
                    "local-dev",
                    "local-dev",
                    "",
                    "failed",
                    "malware-scan",
                    71,
                    false,
                    true,
                    false,
                    "suspicious-imports",
                    false,
                    "sha256-demo-debug-tools-0-9-0",
                    "blocked",
                    DateTimeOffset.UnixEpoch,
                    "Keep quarantined until publisher signs package and scan flags are reviewed.")
            ]);

        Assert.Equal(1, snapshot.FailedToday);
        Assert.Contains("cyclonedx", snapshot.Workers[0].Capabilities);
        Assert.Contains(snapshot.Jobs, job => job.PromotionRing == "blocked" && job.MalwareScan.Contains("suspicious", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    [Trait("Category", "Federation")]
    public void RegistryFederationSnapshotSeparatesAllowedAndBlockedResults()
    {
        var snapshot = new TheUnlocker.Platform.RegistryFederationSnapshot(
            "ui",
            4,
            3,
            1,
            DateTimeOffset.UnixEpoch,
            "team-policy-2026.05",
            "TrustedPublisher",
            [
                new TheUnlocker.Platform.FederatedRegistrySnapshot(
                    "official",
                    "Official Registry",
                    "https://registry.theunlocker.dev",
                    "official",
                    "healthy",
                    "Official signed publishers only",
                    100,
                    false,
                    1842,
                    42,
                    DateTimeOffset.UnixEpoch)
            ],
            [
                new TheUnlocker.Platform.FederatedRegistryResultSnapshot(
                    "better-ui",
                    "Better UI",
                    "1.4.0",
                    "official",
                    "Sample Author",
                    "TrustedPublisher",
                    true,
                    "Signed trusted publisher.",
                    98),
                new TheUnlocker.Platform.FederatedRegistryResultSnapshot(
                    "unsigned-ui-pack",
                    "Unsigned UI Pack",
                    "1.0.0",
                    "community-unity",
                    "unknown",
                    "Unknown",
                    false,
                    "Unsigned community package hidden by current policy.",
                    22)
            ]);

        Assert.Equal(1, snapshot.BlockedResults);
        Assert.Contains(snapshot.Results, result => result.AllowedByPolicy && result.RegistryId == "official");
        Assert.Contains(snapshot.Results, result => !result.AllowedByPolicy && result.PolicyDecision.Contains("Unsigned", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    [Trait("Category", "Trust")]
    public void TrustReputationSnapshotExplainsQuarantineDecision()
    {
        var snapshot = new TheUnlocker.Platform.TrustReputationSnapshot(
            "team-policy-2026.05",
            "TrustedPublisher",
            82,
            1,
            12,
            DateTimeOffset.UnixEpoch,
            [
                new TheUnlocker.Platform.TrustPackageScoreSnapshot(
                    "unsigned-ui-pack",
                    "Unsigned UI Pack",
                    "unknown",
                    "1.0.0",
                    18,
                    "Unknown",
                    "quarantine",
                    "Quarantine until signature, publisher identity, and scan results are available.",
                    [],
                    ["Unknown publisher", "Unsigned package", "No provenance attestation"])
            ],
            [
                new TheUnlocker.Platform.PublisherReputationSnapshot(
                    "sample-author",
                    "Sample Author",
                    "TrustedPublisher",
                    true,
                    18,
                    0,
                    "0.2%",
                    94)
            ],
            [
                new TheUnlocker.Platform.TrustAdvisorySnapshot(
                    "ADV-2026-0007",
                    "debug-tools",
                    "medium",
                    "open",
                    "Network permission added without publisher signature.",
                    DateTimeOffset.UnixEpoch)
            ]);

        Assert.Equal("TrustedPublisher", snapshot.RequiredTrust);
        Assert.Contains(snapshot.Packages, package => package.Decision == "quarantine" && package.RiskFactors.Contains("Unsigned package"));
        Assert.Contains(snapshot.Publishers, publisher => publisher.Verified && publisher.ReputationScore >= 90);
    }

    [Fact]
    [Trait("Category", "CloudModpacks")]
    public void CloudModpackSharingSnapshotKeepsImmutableLockfileAndInstallLink()
    {
        var snapshot = new TheUnlocker.Platform.CloudModpackSharingSnapshot(
            1,
            18420,
            1,
            DateTimeOffset.UnixEpoch,
            [
                new TheUnlocker.Platform.CloudModpackShareSnapshot(
                    "vanilla-plus",
                    "Vanilla+",
                    "2.3.0",
                    ["TheUnlocker Editorial"],
                    "Stable curated Unity starter pack.",
                    "https://registry.theunlocker.dev/modpacks/vanilla-plus/2.3.0/unlocker.lock.json",
                    "sha256-demo-vanilla-plus-lock",
                    "theunlocker://install-pack/vanilla-plus",
                    "verified",
                    "allow",
                    12,
                    148,
                    "2.2.1",
                    "stable",
                    DateTimeOffset.UnixEpoch,
                    ["Signed Only"])
            ]);

        Assert.Equal(1, snapshot.ImmutableLockfiles);
        Assert.StartsWith("theunlocker://install-pack/", snapshot.Modpacks[0].InstallUrl, StringComparison.Ordinal);
        Assert.Contains("sha256", snapshot.Modpacks[0].LockfileSha256, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Category", "AICompatibility")]
    public void AICompatibilityAssistantSnapshotKeepsSuggestionsAndEvidence()
    {
        var snapshot = new TheUnlocker.Platform.AICompatibilityAssistantSnapshot(
            "ai-compat-vanilla-plus-2026-05",
            "Vanilla+ profile",
            "high",
            "medium",
            DateTimeOffset.UnixEpoch,
            "Install bridge-ui-save before enabling Experimental Physics Pack.",
            "One nightly modpack has a repeated crash signature.",
            [
                new TheUnlocker.Platform.AICompatibilitySuggestionSnapshot(
                    "bridge-experimental-physics",
                    "bridge-patch",
                    "warning",
                    "Bridge patch recommended",
                    "Install bridge-ui-save to mitigate the known crash signature.",
                    ["experimental-physics-pack", "legacy-save-tools", "bridge-ui-save"],
                    ["Install bridge-ui-save", "Run compatibility lab"])
            ],
            [
                new TheUnlocker.Platform.AICompatibilityEvidenceSnapshot(
                    "compatibility-lab",
                    "experimental-physics-pack failed with legacy-save-tools crash signature")
            ]);

        Assert.Equal("high", snapshot.Confidence);
        Assert.Contains(snapshot.Suggestions, suggestion => suggestion.Kind == "bridge-patch" && suggestion.Severity == "warning");
        Assert.Contains(snapshot.Evidence, evidence => evidence.Source == "compatibility-lab");
    }

    [Fact]
    [Trait("Category", "PackageDiff")]
    public void PackageDiffSnapshotTracksApprovalSensitiveChanges()
    {
        var snapshot = new TheUnlocker.Platform.PackageDiffSnapshot(
            "better-ui",
            "Better UI",
            "1.3.1",
            "1.4.0",
            "Sample Author",
            DateTimeOffset.UnixEpoch,
            "low-to-medium",
            "requires-approval",
            "Adds a menu permission and settings migration.",
            [
                new TheUnlocker.Platform.PackagePermissionChangeSnapshot(
                    "AddMenuItems",
                    "added",
                    "New command palette entry")
            ],
            [
                new TheUnlocker.Platform.PackageDependencyChangeSnapshot(
                    "shared-ui-core",
                    ">=2.0.0 <3.0.0",
                    ">=2.1.0 <3.0.0",
                    "tightened")
            ],
            [
                new TheUnlocker.Platform.PackageFileChangeSnapshot(
                    "BetterUi.dll",
                    "modified",
                    "sha256-demo-better-ui-1-4-0",
                    42)
            ],
            [
                new TheUnlocker.Platform.PackageSettingsMigrationSnapshot(
                    "better-ui-settings-1-4",
                    "1.3.x",
                    "1.4.0",
                    "dry-run-ready",
                    "Moves toolbar visibility settings.")
            ],
            ["Adds command palette integration."],
            new TheUnlocker.Platform.PackageDiffRollbackSnapshot(
                "1.3.1",
                "rollback-better-ui-1-3-1",
                "restore previous DLL, manifest, assets, and settings backup"));

        Assert.Equal("requires-approval", snapshot.Decision);
        Assert.Contains(snapshot.PermissionChanges, change => change.Permission == "AddMenuItems" && change.Change == "added");
        Assert.Equal("1.3.1", snapshot.Rollback.AvailableVersion);
    }

    [Fact]
    [Trait("Category", "PublisherAnalytics")]
    public void PublisherAnalyticsSnapshotKeepsTrendAndFunnelMetrics()
    {
        var snapshot = new TheUnlocker.Platform.PublisherAnalyticsSnapshot(
            "sample-author",
            "Sample Author",
            "last-30-days",
            DateTimeOffset.UnixEpoch,
            12840,
            9430,
            69810,
            12840,
            "18.4%",
            4.8,
            "0.2%",
            "$0.00",
            [
                new TheUnlocker.Platform.PublisherAnalyticsTrendSnapshot(
                    new DateOnly(2026, 5, 4),
                    2190,
                    1650,
                    5,
                    11920)
            ],
            [
                new TheUnlocker.Platform.PublisherTopModSnapshot(
                    "better-ui",
                    "Better UI",
                    5820,
                    4210,
                    4.9,
                    "0.1%",
                    "22.1%")
            ],
            [
                new TheUnlocker.Platform.PublisherFunnelStageSnapshot(
                    "Install clicks",
                    12840,
                    "18.4%")
            ],
            [
                new TheUnlocker.Platform.PublisherVersionAdoptionSnapshot(
                    "1.4.0",
                    "stable",
                    7210,
                    "56.2%")
            ],
            [
                new TheUnlocker.Platform.PublisherModerationOutcomeSnapshot(
                    "approved",
                    18,
                    2.4)
            ]);

        Assert.Equal("18.4%", snapshot.ConversionRate);
        Assert.Contains(snapshot.TopMods, mod => mod.ModId == "better-ui" && mod.Rating > 4.5);
        Assert.Contains(snapshot.Funnel, stage => stage.Stage == "Install clicks");
    }

    [Fact]
    [Trait("Category", "Policy")]
    public void PolicySimulationSnapshotKeepsDecisionFindingsAndSecurityRules()
    {
        var snapshot = new TheUnlocker.Platform.PolicySimulationSnapshot(
            "2026.05.04-enterprise",
            "studio-private",
            DateTimeOffset.UnixEpoch,
            "review",
            1,
            1,
            1,
            [
                new TheUnlocker.Platform.PolicySimulationRuleSnapshot(
                    "signature-required",
                    "Require trusted signatures",
                    true,
                    "block",
                    "Unsigned packages from non-local registries are blocked before install.")
            ],
            [
                new TheUnlocker.Platform.PolicySimulationScenarioSnapshot(
                    "unsigned-ui-community",
                    "Unsigned community UI pack",
                    "unsigned-ui-pack",
                    "1.0.0",
                    "community-unity",
                    "Unknown",
                    ["AddMenuItems"],
                    "stable",
                    "block",
                    18,
                    [
                        new TheUnlocker.Platform.PolicySimulationFindingSnapshot(
                            "signature-required",
                            "critical",
                            "Package is unsigned and publisher identity is unknown.")
                    ])
            ],
            ["Keep unsigned community packages hidden until publisher verification and signatures are available."]);

        Assert.Equal("review", snapshot.OverallDecision);
        Assert.Contains(snapshot.Rules, rule => rule.Enabled && rule.Severity == "block");
        Assert.Contains(snapshot.Scenarios, scenario => scenario.Decision == "block" && scenario.Findings.Any(finding => finding.Severity == "critical"));
    }

    private static string CreateTempDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"the-unlocker-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        return directory;
    }
}
