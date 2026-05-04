namespace TheUnlocker.Platform;

public sealed record DeviceSession(
    string Id,
    string DeviceName,
    string LastIpAddress,
    DateTimeOffset LastSeenAt,
    bool IsTrusted,
    bool IsRevoked);

public sealed record AccountSecurityState(
    string UserId,
    string Email,
    bool EmailVerified,
    IReadOnlyList<DeviceSession> DeviceSessions,
    IReadOnlyList<string> LoginAuditEvents);

public sealed record PasswordResetTicket(
    string UserId,
    string TokenId,
    DateTimeOffset ExpiresAt,
    bool Used);

public sealed record EmailVerificationTicket(
    string UserId,
    string Email,
    string TokenId,
    DateTimeOffset ExpiresAt,
    bool Used);

public sealed record CloudSyncSnapshot(
    string UserId,
    IReadOnlyList<string> InstalledMods,
    IReadOnlyDictionary<string, string[]> Profiles,
    IReadOnlyList<string> Favorites,
    IReadOnlyList<string> TrustDecisions,
    IReadOnlyList<string> RecoveryHistory);

public enum InstallGateStatus
{
    Ready,
    Waiting,
    RequiresApproval,
    Blocked
}

public sealed record InstallPipelineGate(
    string Id,
    string Name,
    InstallGateStatus Status,
    string Description);

public sealed class RealModInstallPipeline
{
    public IReadOnlyList<InstallPipelineGate> BuildDefaultPlan()
    {
        return
        [
            new("download", "Download", InstallGateStatus.Ready, "Resolve package source and stream it into staging."),
            new("hash", "Hash verify", InstallGateStatus.Ready, "Compare SHA-256 against package metadata or lockfile pins."),
            new("signature", "Signature verify", InstallGateStatus.Ready, "Validate publisher signature and trusted key policy."),
            new("scan", "Scan", InstallGateStatus.Waiting, "Run manifest validation, malware scanners, SBOM checks, and risk scoring."),
            new("dependencies", "Dependency resolve", InstallGateStatus.Ready, "Resolve required, optional, peer, SDK, runtime, and game constraints."),
            new("permissions", "Permissions approval", InstallGateStatus.RequiresApproval, "Require consent when a package requests new or risky permissions."),
            new("install", "Atomic install", InstallGateStatus.Waiting, "Promote staged files into the active mod directory."),
            new("rollback", "Rollback point", InstallGateStatus.Ready, "Record prior package state for one-click restore.")
        ];
    }
}

public sealed record PublisherPortalSummary(
    string PublisherId,
    string DisplayName,
    bool Verified,
    int ModCount,
    int PendingUploads,
    int OpenCrashReports,
    IReadOnlyList<string> SigningKeys,
    IReadOnlyList<string> ModerationStatuses);

public sealed record ModpackStudioDraft(
    string Id,
    string Name,
    IReadOnlyList<string> Mods,
    IReadOnlyList<string> CompatibilityWarnings,
    string LockfilePreview,
    string ShareUrl);

public sealed record GraphNode(string Id, string Label, string Kind);

public sealed record GraphEdge(string From, string To, string Label);

public sealed record LiveDependencyGraph(
    IReadOnlyList<GraphNode> Nodes,
    IReadOnlyList<GraphEdge> Edges);

public sealed record RiskScoreExplanation(
    string PackageId,
    int Score,
    IReadOnlyList<string> PositiveFactors,
    IReadOnlyList<string> RiskFactors);

public sealed class RiskScoreExplainer
{
    public RiskScoreExplanation Explain(
        string packageId,
        bool signedByTrustedPublisher,
        bool hasUnsafePermissions,
        int recentCrashCount,
        bool hasSuspiciousImports,
        bool unsignedBinary)
    {
        var positives = new List<string>();
        var risks = new List<string>();
        var score = 50;

        if (signedByTrustedPublisher)
        {
            score += 20;
            positives.Add("Signed by a trusted publisher.");
        }
        else
        {
            risks.Add("Publisher signature is missing or untrusted.");
        }

        if (hasUnsafePermissions)
        {
            score -= 15;
            risks.Add("Requests high-risk permissions.");
        }
        else
        {
            score += 10;
            positives.Add("Permissions stay within approved scopes.");
        }

        if (recentCrashCount > 0)
        {
            score -= Math.Min(25, recentCrashCount * 5);
            risks.Add($"Recent crash reports: {recentCrashCount}.");
        }
        else
        {
            positives.Add("No recent crash reports.");
        }

        if (hasSuspiciousImports)
        {
            score -= 20;
            risks.Add("Package contains suspicious binary imports.");
        }

        if (unsignedBinary)
        {
            score -= 10;
            risks.Add("Contains an unsigned binary payload.");
        }

        return new RiskScoreExplanation(packageId, Math.Clamp(score, 0, 100), positives, risks);
    }
}

public sealed record CrashRecoveryStep(string Id, string Label, string Description);

public sealed class CrashRecoveryWizard
{
    public IReadOnlyList<CrashRecoveryStep> BuildPlan()
    {
        return
        [
            new("safe-mode", "Start safe mode", "Disable all mods for the next launch."),
            new("disable-recent", "Disable recent changes", "Turn off mods changed shortly before the crash."),
            new("rollback", "Rollback updates", "Restore the last healthy package version."),
            new("logs", "Inspect logs", "Open filtered logs around the crash timestamp."),
            new("submit", "Submit diagnostics", "Upload a diagnostics bundle to the registry.")
        ];
    }
}

public sealed record PluginMarketplaceEntry(
    string Id,
    string Name,
    string ExtensionType,
    string Publisher,
    string TrustLevel,
    string Description);

public sealed record WorkflowRule(
    string Id,
    string Trigger,
    string Condition,
    string Action,
    bool Enabled);

public sealed record InstallQueueSnapshot(
    string Id,
    string PackageId,
    string Version,
    string Status,
    string CurrentStage,
    int Progress,
    string RollbackState,
    DateTimeOffset CreatedAt);

public sealed record PublisherAnalyticsSummary(
    string PublisherId,
    int MonthlyInstalls,
    string ConversionRate,
    double AverageRating,
    int PendingUploads,
    int OpenCrashReports,
    IReadOnlyList<string> ModerationStates,
    IReadOnlyList<string> AnalyticsSegments);

public sealed record ModerationQueueItem(
    string Id,
    string PackageId,
    string Publisher,
    string Status,
    int RiskScore,
    IReadOnlyList<string> Flags,
    DateTimeOffset SubmittedAt);

public sealed record PlatformNotification(
    string Id,
    string Severity,
    string Title,
    string Body,
    DateTimeOffset CreatedAt);

public sealed record EffectivePolicySnapshot(
    string Source,
    string Version,
    bool AllowUnsignedMods,
    string RequiredTrustLevel,
    IReadOnlyList<string> AllowedRegistries,
    IReadOnlyList<string> BlockedPermissions,
    IReadOnlyList<string> BlockedMods,
    DateTimeOffset LastSyncedAt);

public sealed record RegistryServiceHealth(
    string Service,
    string Status,
    int LatencyMs,
    string Detail);

public sealed record DesktopReleasePolicy(
    bool RequireSignedUpdates,
    bool AllowPrerelease,
    bool RollbackOnFailure,
    IReadOnlyList<string> AllowedChannels);

public sealed record DesktopReleaseSnapshot(
    string CurrentVersion,
    string ActiveChannel,
    bool AutoUpdate,
    DesktopReleasePolicy Policy,
    IReadOnlyList<string> ReleaseVersions,
    string RollbackPlan);

public sealed record FleetCommand(
    string Id,
    string Type,
    string Status,
    string Target);

public sealed record FleetDeviceSnapshot(
    string Id,
    string Name,
    string Kind,
    string Status,
    DateTimeOffset LastSeenAt,
    string ActiveProfile,
    int InstalledMods,
    string TrustLevel,
    bool CanReceiveCommands,
    IReadOnlyList<FleetCommand> PendingCommands);

public sealed record DeviceFleetSnapshot(
    string AccountId,
    int OnlineDevices,
    int PendingCommands,
    DateTimeOffset LastFleetSyncAt,
    string OrchestrationKey,
    IReadOnlyList<FleetDeviceSnapshot> Devices);

public sealed record CompatibilityLabAdapterSnapshot(
    string Id,
    string Name,
    string Status,
    IReadOnlyList<string> SupportedVersions,
    DateTimeOffset LastTestAt);

public sealed record CompatibilityLabJobSnapshot(
    string Id,
    string ModpackId,
    string GameId,
    string Adapter,
    string Status,
    string Result,
    int DurationSeconds,
    DateTimeOffset StartedAt,
    IReadOnlyList<string> Findings,
    string CrashSignature,
    string Recommendation);

public sealed record CompatibilityLabQueueSnapshot(
    int Queued,
    int Running,
    int Failed,
    int DeadLetter,
    int AverageSeconds);

public sealed record CompatibilityLabSnapshot(
    int ActiveJobs,
    int QueuedJobs,
    int FailedJobs,
    string PassRate,
    int AdaptersCovered,
    DateTimeOffset LastRunAt,
    CompatibilityLabQueueSnapshot Queue,
    IReadOnlyList<CompatibilityLabAdapterSnapshot> Adapters,
    IReadOnlyList<CompatibilityLabJobSnapshot> Jobs);

public sealed record BuildFarmWorkerSnapshot(
    string Id,
    string Pool,
    string Status,
    string CurrentJob,
    IReadOnlyList<string> Capabilities,
    DateTimeOffset LastHeartbeatAt);

public sealed record BuildFarmJobSnapshot(
    string Id,
    string PackageId,
    string Version,
    string Publisher,
    string SourceCommit,
    string CiRunUrl,
    string Status,
    string Stage,
    int DurationSeconds,
    bool Reproducible,
    bool SbomGenerated,
    bool SignatureVerified,
    string MalwareScan,
    bool ProvenanceAttested,
    string ArtifactSha256,
    string PromotionRing,
    DateTimeOffset StartedAt,
    string ReleaseRecommendation);

public sealed record BuildFarmSnapshot(
    int ActiveJobs,
    int QueuedJobs,
    int SuccessfulToday,
    int FailedToday,
    string AverageBuildTime,
    DateTimeOffset LastCompletedAt,
    IReadOnlyList<BuildFarmWorkerSnapshot> Workers,
    IReadOnlyList<BuildFarmJobSnapshot> Jobs);

public sealed record FederatedRegistrySnapshot(
    string Id,
    string Name,
    string Url,
    string Kind,
    string Status,
    string TrustPolicy,
    int Priority,
    bool AllowUnsigned,
    int PackagesIndexed,
    int LatencyMs,
    DateTimeOffset LastSyncAt);

public sealed record FederatedRegistryResultSnapshot(
    string PackageId,
    string Name,
    string Version,
    string RegistryId,
    string Publisher,
    string TrustLevel,
    bool AllowedByPolicy,
    string PolicyDecision,
    int Score);

public sealed record RegistryFederationSnapshot(
    string Query,
    int Connected,
    int Healthy,
    int BlockedResults,
    DateTimeOffset LastFederatedAt,
    string PolicyVersion,
    string DefaultTrustLevel,
    IReadOnlyList<FederatedRegistrySnapshot> Registries,
    IReadOnlyList<FederatedRegistryResultSnapshot> Results);

public sealed record TrustPackageScoreSnapshot(
    string PackageId,
    string Name,
    string Publisher,
    string Version,
    int Score,
    string TrustLevel,
    string Decision,
    string Recommendation,
    IReadOnlyList<string> PositiveFactors,
    IReadOnlyList<string> RiskFactors);

public sealed record PublisherReputationSnapshot(
    string PublisherId,
    string DisplayName,
    string TrustLevel,
    bool Verified,
    int SignedReleases,
    int ActiveAdvisories,
    string CrashRate,
    int ReputationScore);

public sealed record TrustAdvisorySnapshot(
    string Id,
    string PackageId,
    string Severity,
    string Status,
    string Summary,
    DateTimeOffset PublishedAt);

public sealed record TrustReputationSnapshot(
    string PolicyVersion,
    string RequiredTrust,
    int AverageScore,
    int FlaggedPackages,
    int TrustedPublishers,
    DateTimeOffset LastEvaluatedAt,
    IReadOnlyList<TrustPackageScoreSnapshot> Packages,
    IReadOnlyList<PublisherReputationSnapshot> Publishers,
    IReadOnlyList<TrustAdvisorySnapshot> Advisories);

public sealed record CloudModpackShareSnapshot(
    string Id,
    string Name,
    string Version,
    IReadOnlyList<string> Maintainers,
    string Description,
    string LockfileUrl,
    string LockfileSha256,
    string InstallUrl,
    string Compatibility,
    string TrustDecision,
    int ModCount,
    int DownloadSizeMb,
    string RollbackVersion,
    string UpdateRing,
    DateTimeOffset LastCompatibilityAt,
    IReadOnlyList<string> Badges);

public sealed record CloudModpackSharingSnapshot(
    int FeaturedCount,
    int SharedInstalls,
    int ImmutableLockfiles,
    DateTimeOffset LastIndexedAt,
    IReadOnlyList<CloudModpackShareSnapshot> Modpacks);

public sealed record AICompatibilitySuggestionSnapshot(
    string Id,
    string Kind,
    string Severity,
    string Title,
    string Detail,
    IReadOnlyList<string> AffectedIds,
    IReadOnlyList<string> Actions);

public sealed record AICompatibilityEvidenceSnapshot(
    string Source,
    string Signal);

public sealed record AICompatibilityAssistantSnapshot(
    string AnalysisId,
    string Subject,
    string Confidence,
    string OverallRisk,
    DateTimeOffset GeneratedAt,
    string RecommendedAction,
    string Summary,
    IReadOnlyList<AICompatibilitySuggestionSnapshot> Suggestions,
    IReadOnlyList<AICompatibilityEvidenceSnapshot> Evidence);

public sealed record PackagePermissionChangeSnapshot(
    string Permission,
    string Change,
    string Reason);

public sealed record PackageDependencyChangeSnapshot(
    string Dependency,
    string From,
    string To,
    string Change);

public sealed record PackageFileChangeSnapshot(
    string Path,
    string Change,
    string Sha256,
    int SizeDeltaKb);

public sealed record PackageSettingsMigrationSnapshot(
    string Id,
    string From,
    string To,
    string Status,
    string Description);

public sealed record PackageDiffRollbackSnapshot(
    string AvailableVersion,
    string SnapshotId,
    string Strategy);

public sealed record PackageDiffSnapshot(
    string PackageId,
    string Name,
    string FromVersion,
    string ToVersion,
    string Publisher,
    DateTimeOffset GeneratedAt,
    string RiskChange,
    string Decision,
    string Summary,
    IReadOnlyList<PackagePermissionChangeSnapshot> PermissionChanges,
    IReadOnlyList<PackageDependencyChangeSnapshot> DependencyChanges,
    IReadOnlyList<PackageFileChangeSnapshot> FileChanges,
    IReadOnlyList<PackageSettingsMigrationSnapshot> SettingsMigrations,
    IReadOnlyList<string> Changelog,
    PackageDiffRollbackSnapshot Rollback);

public sealed record PublisherAnalyticsTrendSnapshot(
    DateOnly Date,
    int Installs,
    int Updates,
    int Crashes,
    int Views);

public sealed record PublisherTopModSnapshot(
    string ModId,
    string Name,
    int Installs,
    int Updates,
    double Rating,
    string CrashRate,
    string ConversionRate);

public sealed record PublisherFunnelStageSnapshot(
    string Stage,
    int Count,
    string Rate);

public sealed record PublisherVersionAdoptionSnapshot(
    string Version,
    string Ring,
    int Users,
    string Percentage);

public sealed record PublisherModerationOutcomeSnapshot(
    string Status,
    int Count,
    double AverageHours);

public sealed record PublisherAnalyticsSnapshot(
    string PublisherId,
    string DisplayName,
    string Period,
    DateTimeOffset GeneratedAt,
    int Installs,
    int Updates,
    int MarketplaceViews,
    int InstallClicks,
    string ConversionRate,
    double AverageRating,
    string CrashRate,
    string RevenueEstimate,
    IReadOnlyList<PublisherAnalyticsTrendSnapshot> Trend,
    IReadOnlyList<PublisherTopModSnapshot> TopMods,
    IReadOnlyList<PublisherFunnelStageSnapshot> Funnel,
    IReadOnlyList<PublisherVersionAdoptionSnapshot> Adoption,
    IReadOnlyList<PublisherModerationOutcomeSnapshot> ModerationOutcomes);

public sealed record PolicySimulationRuleSnapshot(
    string Id,
    string Label,
    bool Enabled,
    string Severity,
    string Description);

public sealed record PolicySimulationFindingSnapshot(
    string RuleId,
    string Severity,
    string Message);

public sealed record PolicySimulationScenarioSnapshot(
    string Id,
    string Title,
    string PackageId,
    string Version,
    string Registry,
    string TrustLevel,
    IReadOnlyList<string> RequestedPermissions,
    string UpdateRing,
    string Decision,
    int Score,
    IReadOnlyList<PolicySimulationFindingSnapshot> Findings);

public sealed record PolicySimulationSnapshot(
    string PolicyVersion,
    string Environment,
    DateTimeOffset GeneratedAt,
    string OverallDecision,
    int AllowedCount,
    int ReviewCount,
    int BlockedCount,
    IReadOnlyList<PolicySimulationRuleSnapshot> Rules,
    IReadOnlyList<PolicySimulationScenarioSnapshot> Scenarios,
    IReadOnlyList<string> RecommendedActions);

public sealed record CompatibilitySignal(
    string ModA,
    string ModB,
    int InstallCount,
    int CrashCount,
    string Recommendation);

public sealed record PublisherTrustVerification(
    string PublisherId,
    bool DomainVerified,
    bool GitHubOrganizationVerified,
    bool SignedReleases,
    IReadOnlyList<string> Badges);

public sealed record MarketplaceCollection(
    string Id,
    string Name,
    string Curator,
    IReadOnlyList<string> ModIds,
    string Description);

public sealed record MarketplaceCollectionBadge(
    string CollectionId,
    string Badge,
    string Reason);

public sealed record CompatibilityIntelligenceSummary(
    IReadOnlyList<CompatibilitySignal> Signals,
    IReadOnlyList<string> KnownBridgePatches,
    IReadOnlyList<string> SuggestedLoadOrderRules);

public sealed record DocumentationHubLink(
    string Title,
    string Path,
    string Description);
