export type RegistryMod = {
  id: string;
  name: string;
  author: string;
  description: string;
  status: string;
  gameId: string;
  trustLevel: string;
  tags: string[];
  permissions: string[];
  versions: RegistryVersion[];
  dependencies?: string[];
  screenshots?: string[];
  rating?: number;
  installCount?: number;
  publisher?: PublisherProfile;
  reviews?: ModReview[];
};

export type RegistryVersion = {
  version: string;
  downloadUrl: string;
  sha256: string;
  changelog: string;
  createdAt: string;
};

export type RegistryHealth = {
  status: string;
  registry: string;
  redis: string;
  mongo: string;
  minio: string;
  checkedAt: string;
};

export type AccountUser = {
  id: string;
  email: string;
  displayName: string;
  onboardingComplete: boolean;
  role: string;
  primaryGame: string;
  registryUrl: string;
  trustedDevices?: string[];
  emailVerified?: boolean;
};

export type AuthSession = {
  token: string;
  refreshToken?: string;
  expiresAt: string;
  refreshExpiresAt?: string;
  user: AccountUser;
};

export type AuthMode = 'signin' | 'create' | 'reset';

export type PublisherProfile = {
  id: string;
  name: string;
  verified: boolean;
  publicKey: string;
  trustHistory: string[];
  stats: {
    mods: number;
    installs: number;
    crashRate: string;
  };
};

export type ModReview = {
  id: string;
  author: string;
  rating: number;
  body: string;
  status: 'visible' | 'flagged' | 'hidden';
  publisherReply?: string;
};

export type MarketplacePage = 'mods' | 'settings' | 'publisher' | 'analytics' | 'policylab' | 'modpack' | 'cloudpacks' | 'assistant' | 'diff' | 'operations' | 'platform' | 'docs' | 'collections' | 'compatibility' | 'lab' | 'builds' | 'federation' | 'trust' | 'control' | 'governance' | 'releases' | 'devices';

export type PlatformUpgrade = {
  id: string;
  name: string;
  status: string;
  description: string;
  surfaces: string[];
};

export type ProductUpgrade = {
  id: string;
  name: string;
  category: string;
  status: string;
  description: string;
  actions: string[];
  metrics: { label: string; value: string }[];
};

export type InstallPipelineStep = {
  id: string;
  name: string;
  status: 'ready' | 'waiting' | 'requires-approval' | 'blocked';
  description: string;
};

export type DependencyGraph = {
  nodes: { id: string; label: string; kind: string }[];
  edges: { from: string; to: string; label: string }[];
};

export type DocumentationLink = {
  title: string;
  href: string;
  description: string;
};

export type AccountSecurityState = {
  emailVerified: boolean;
  trustedDevices: string[];
  sessions: { id: string; createdAt: string; expiresAt: string; revoked: boolean }[];
  loginAudit: { action: string; success: boolean; ip: string; userAgent: string; createdAt: string }[];
};

export type MarketplaceCollection = {
  id: string;
  name: string;
  curator: string;
  description: string;
  modIds: string[];
  badges: string[];
};

export type CompatibilitySignal = {
  modA: string;
  modB: string;
  installCount: number;
  crashCount: number;
  risk: string;
  recommendation: string;
};

export type InstallQueueItem = {
  id: string;
  packageId: string;
  version: string;
  status: string;
  currentStage: string;
  progress: number;
  rollback: string;
  createdAt: string;
};

export type PublisherDashboard = {
  publisherId: string;
  displayName: string;
  verified: boolean;
  mods: number;
  pendingUploads: number;
  openCrashReports: number;
  monthlyInstalls: number;
  conversionRate: string;
  averageRating: number;
  signingKeys: string[];
  moderationStates: string[];
  analyticsSegments: string[];
};

export type PublisherTrendPoint = {
  date: string;
  installs: number;
  updates: number;
  crashes: number;
  views: number;
};

export type PublisherTopMod = {
  modId: string;
  name: string;
  installs: number;
  updates: number;
  rating: number;
  crashRate: string;
  conversionRate: string;
};

export type PublisherFunnelStage = {
  stage: string;
  count: number;
  rate: string;
};

export type PublisherVersionAdoption = {
  version: string;
  ring: string;
  users: number;
  percentage: string;
};

export type PublisherModerationOutcome = {
  status: string;
  count: number;
  averageHours: number;
};

export type PublisherAnalyticsState = {
  publisherId: string;
  displayName: string;
  period: string;
  generatedAt: string;
  installs: number;
  updates: number;
  marketplaceViews: number;
  installClicks: number;
  conversionRate: string;
  averageRating: number;
  crashRate: string;
  revenueEstimate: string;
  trend: PublisherTrendPoint[];
  topMods: PublisherTopMod[];
  funnel: PublisherFunnelStage[];
  adoption: PublisherVersionAdoption[];
  moderationOutcomes: PublisherModerationOutcome[];
};

export type RecoveryStep = {
  id: string;
  label: string;
  description: string;
};

export type WorkflowRule = {
  id: string;
  trigger: string;
  condition: string;
  action: string;
  enabled: boolean;
};

export type ModerationQueueItem = {
  id: string;
  packageId: string;
  publisher: string;
  status: string;
  riskScore: number;
  flags: string[];
  submittedAt: string;
};

export type PlatformNotification = {
  id: string;
  severity: 'info' | 'warning' | 'critical';
  title: string;
  body: string;
  createdAt: string;
};

export type EffectivePolicy = {
  source: string;
  version: string;
  allowUnsignedMods: boolean;
  requiredTrustLevel: string;
  allowedRegistries: string[];
  blockedPermissions: string[];
  blockedMods: string[];
  lastSyncedAt: string;
  nextSyncRecommended: string;
};

export type RegistryServiceHealth = {
  service: string;
  status: string;
  latencyMs: number;
  detail: string;
};

export type DesktopRelease = {
  version: string;
  channel: 'stable' | 'beta' | 'nightly';
  downloadUrl: string;
  sha256: string;
  signatureUrl: string;
  changelog: string;
  health: string;
  signed: boolean;
  rolloutPercentage: number;
  publishedAt: string;
};

export type DesktopReleasePolicy = {
  requireSignedUpdates: boolean;
  allowPrerelease: boolean;
  rollbackOnFailure: boolean;
  allowedChannels: string[];
};

export type DesktopRollbackState = {
  availableVersion: string;
  lastHealthyAt: string;
  plan: string;
};

export type DesktopReleaseState = {
  currentVersion: string;
  activeChannel: string;
  autoUpdate: boolean;
  lastCheckedAt: string;
  policy: DesktopReleasePolicy;
  releases: DesktopRelease[];
  rollback: DesktopRollbackState;
};

export type FleetCommand = {
  id: string;
  type: string;
  status: string;
  target: string;
};

export type FleetDevice = {
  id: string;
  name: string;
  kind: string;
  status: string;
  lastSeenAt: string;
  registryUrl: string;
  activeProfile: string;
  installedMods: number;
  trustLevel: string;
  canReceiveCommands: boolean;
  lanAddress: string;
  pendingCommands: FleetCommand[];
};

export type DeviceFleetState = {
  accountId: string;
  onlineDevices: number;
  pendingCommands: number;
  lastFleetSyncAt: string;
  orchestrationKey: string;
  devices: FleetDevice[];
};

export type CompatibilityLabAdapter = {
  id: string;
  name: string;
  status: string;
  supportedVersions: string[];
  lastTestAt: string;
};

export type CompatibilityLabJob = {
  id: string;
  modpackId: string;
  gameId: string;
  adapter: string;
  status: string;
  result: 'passed' | 'failed' | 'pending' | 'warning';
  durationSeconds: number;
  startedAt: string;
  findings: string[];
  crashSignature: string;
  recommendation: string;
};

export type CompatibilityLabState = {
  activeJobs: number;
  queuedJobs: number;
  failedJobs: number;
  passRate: string;
  adaptersCovered: number;
  lastRunAt: string;
  queue: {
    queued: number;
    running: number;
    failed: number;
    deadLetter: number;
    averageSeconds: number;
  };
  adapters: CompatibilityLabAdapter[];
  jobs: CompatibilityLabJob[];
};

export type BuildFarmWorker = {
  id: string;
  pool: string;
  status: string;
  currentJob: string;
  capabilities: string[];
  lastHeartbeatAt: string;
};

export type BuildFarmJob = {
  id: string;
  packageId: string;
  version: string;
  publisher: string;
  sourceCommit: string;
  ciRunUrl: string;
  status: string;
  stage: string;
  durationSeconds: number;
  reproducible: boolean;
  sbomGenerated: boolean;
  signatureVerified: boolean;
  malwareScan: string;
  provenanceAttested: boolean;
  artifactSha256: string;
  promotionRing: string;
  startedAt: string;
  releaseRecommendation: string;
};

export type BuildFarmState = {
  activeJobs: number;
  queuedJobs: number;
  successfulToday: number;
  failedToday: number;
  averageBuildTime: string;
  lastCompletedAt: string;
  workers: BuildFarmWorker[];
  jobs: BuildFarmJob[];
};

export type FederatedRegistry = {
  id: string;
  name: string;
  url: string;
  kind: string;
  status: string;
  trustPolicy: string;
  priority: number;
  allowUnsigned: boolean;
  packagesIndexed: number;
  latencyMs: number;
  lastSyncAt: string;
};

export type FederatedSearchResult = {
  packageId: string;
  name: string;
  version: string;
  registryId: string;
  publisher: string;
  trustLevel: string;
  allowedByPolicy: boolean;
  policyDecision: string;
  score: number;
};

export type RegistryFederationState = {
  query: string;
  connected: number;
  healthy: number;
  blockedResults: number;
  lastFederatedAt: string;
  policyVersion: string;
  defaultTrustLevel: string;
  registries: FederatedRegistry[];
  results: FederatedSearchResult[];
};

export type TrustPackageScore = {
  packageId: string;
  name: string;
  publisher: string;
  version: string;
  score: number;
  trustLevel: string;
  decision: string;
  recommendation: string;
  positiveFactors: string[];
  riskFactors: string[];
};

export type PublisherReputation = {
  publisherId: string;
  displayName: string;
  trustLevel: string;
  verified: boolean;
  signedReleases: number;
  activeAdvisories: number;
  crashRate: string;
  reputationScore: number;
};

export type VulnerabilityAdvisory = {
  id: string;
  packageId: string;
  severity: string;
  status: string;
  summary: string;
  publishedAt: string;
};

export type TrustReputationState = {
  policyVersion: string;
  requiredTrust: string;
  averageScore: number;
  flaggedPackages: number;
  trustedPublishers: number;
  lastEvaluatedAt: string;
  packages: TrustPackageScore[];
  publishers: PublisherReputation[];
  advisories: VulnerabilityAdvisory[];
};

export type PolicySimulationRule = {
  id: string;
  label: string;
  enabled: boolean;
  severity: string;
  description: string;
};

export type PolicySimulationFinding = {
  ruleId: string;
  severity: string;
  message: string;
};

export type PolicySimulationScenario = {
  id: string;
  title: string;
  packageId: string;
  version: string;
  registry: string;
  trustLevel: string;
  requestedPermissions: string[];
  updateRing: string;
  decision: string;
  score: number;
  findings: PolicySimulationFinding[];
};

export type PolicySimulationState = {
  policyVersion: string;
  environment: string;
  generatedAt: string;
  overallDecision: string;
  allowedCount: number;
  reviewCount: number;
  blockedCount: number;
  rules: PolicySimulationRule[];
  scenarios: PolicySimulationScenario[];
  recommendedActions: string[];
};

export type CloudModpack = {
  id: string;
  name: string;
  version: string;
  maintainers: string[];
  description: string;
  lockfileUrl: string;
  lockfileSha256: string;
  installUrl: string;
  compatibility: string;
  trustDecision: string;
  modCount: number;
  downloadSizeMb: number;
  rollbackVersion: string;
  updateRing: string;
  lastCompatibilityAt: string;
  badges: string[];
};

export type CloudModpackState = {
  featuredCount: number;
  sharedInstalls: number;
  immutableLockfiles: number;
  lastIndexedAt: string;
  modpacks: CloudModpack[];
};

export type AICompatibilitySuggestion = {
  id: string;
  kind: string;
  severity: string;
  title: string;
  detail: string;
  affectedIds: string[];
  actions: string[];
};

export type AICompatibilityEvidence = {
  source: string;
  signal: string;
};

export type AICompatibilityAssistantState = {
  analysisId: string;
  subject: string;
  confidence: string;
  overallRisk: string;
  generatedAt: string;
  recommendedAction: string;
  summary: string;
  suggestions: AICompatibilitySuggestion[];
  evidence: AICompatibilityEvidence[];
};

export type PackagePermissionChange = {
  permission: string;
  change: string;
  reason: string;
};

export type PackageDependencyChange = {
  dependency: string;
  from: string;
  to: string;
  change: string;
};

export type PackageFileChange = {
  path: string;
  change: string;
  sha256: string;
  sizeDeltaKb: number;
};

export type PackageSettingsMigration = {
  id: string;
  from: string;
  to: string;
  status: string;
  description: string;
};

export type PackageDiffRollback = {
  availableVersion: string;
  snapshotId: string;
  strategy: string;
};

export type PackageDiffState = {
  packageId: string;
  name: string;
  fromVersion: string;
  toVersion: string;
  publisher: string;
  generatedAt: string;
  riskChange: string;
  decision: string;
  summary: string;
  permissionChanges: PackagePermissionChange[];
  dependencyChanges: PackageDependencyChange[];
  fileChanges: PackageFileChange[];
  settingsMigrations: PackageSettingsMigration[];
  changelog: string[];
  rollback: PackageDiffRollback;
};
