import type {
  AuthSession,
  AICompatibilityAssistantState,
  BuildFarmState,
  CloudModpackState,
  CompatibilityLabState,
  AccountSecurityState,
  CompatibilitySignal,
  DependencyGraph,
  DocumentationLink,
  DesktopReleaseState,
  DeviceFleetState,
  EffectivePolicy,
  InstallPipelineStep,
  InstallQueueItem,
  MarketplaceCollection,
  ModerationQueueItem,
  PackageDiffState,
  PolicySimulationState,
  PlatformUpgrade,
  PlatformNotification,
  ProductUpgrade,
  PublisherAnalyticsState,
  PublisherDashboard,
  RecoveryStep,
  RegistryHealth,
  RegistryFederationState,
  RegistryServiceHealth,
  RegistryMod,
  TrustReputationState,
  WorkflowRule,
} from './types';

const apiBase = '/go-api/api/v1';

const fallbackMods: RegistryMod[] = [
  {
    id: 'hello-world',
    name: 'Hello World',
    author: 'Sample Author',
    description: 'A safe starter mod that registers a menu item and notification.',
    status: 'Approved',
    gameId: 'unity',
    trustLevel: 'Trusted Publisher',
    tags: ['sample', 'sdk'],
    permissions: ['AddMenuItems', 'SendNotifications'],
    versions: [
      {
        version: '1.0.0',
        downloadUrl: '#',
        sha256: '',
        changelog: 'Initial release',
        createdAt: new Date().toISOString(),
      },
    ],
  },
];

export async function fetchMods(query: URLSearchParams): Promise<RegistryMod[]> {
  try {
    const response = await fetch(`${apiBase}/mods?${query.toString()}`);
    if (!response.ok) {
      throw new Error(`Registry returned ${response.status}`);
    }
    const mods = await response.json() as RegistryMod[];
    return rankMods(mods.map(enrichMod), query.get('q') ?? '');
  } catch {
    return rankMods(fallbackMods.map(enrichMod), query.get('q') ?? '');
  }
}

export async function fetchPlatformUpgrades(): Promise<PlatformUpgrade[]> {
  const fallback: PlatformUpgrade[] = [
    {
      id: 'federation',
      name: 'Real Multi-Registry Federation',
      status: 'runtime-ready',
      description: 'Search official, private, local dev, and game community registries with policy-aware results.',
      surfaces: ['desktop', 'go-api', 'runtime'],
    },
    {
      id: 'sat-solver',
      name: 'Mod Dependency SAT Solver',
      status: 'runtime-ready',
      description: 'Resolve version ranges, conflicts, optional dependencies, peer dependencies, game constraints, SDK constraints, and lockfiles.',
      surfaces: ['desktop', 'cli', 'runtime'],
    },
  ];

  try {
    const response = await fetch(`${apiBase}/platform/major-upgrades`);
    if (!response.ok) {
      throw new Error(`Registry returned ${response.status}`);
    }
    return await response.json() as PlatformUpgrade[];
  } catch {
    return fallback;
  }
}

export async function fetchProductUpgrades(): Promise<ProductUpgrade[]> {
  const fallback: ProductUpgrade[] = [
    {
      id: 'real-account-auth',
      name: 'Real Account Auth',
      category: 'Identity',
      status: 'gateway-ready',
      description: 'Refresh tokens, device sessions, password reset hooks, email verification state, trusted devices, and login audit logs.',
      actions: ['Rotate refresh tokens', 'Revoke device sessions', 'Record login audit events'],
      metrics: [
        { label: 'Session TTL', value: '24h access / 30d refresh' },
        { label: 'Hashing', value: 'bcrypt' },
      ],
    },
  ];

  try {
    const response = await fetch(`${apiBase}/platform/product-upgrades`);
    if (!response.ok) {
      throw new Error(`Registry returned ${response.status}`);
    }
    return await response.json() as ProductUpgrade[];
  } catch {
    return fallback;
  }
}

export async function fetchInstallPipeline(): Promise<InstallPipelineStep[]> {
  const fallback: InstallPipelineStep[] = [
    { id: 'download', name: 'Download', status: 'ready', description: 'Resolve package source and stream to staging.' },
    { id: 'hash', name: 'Hash verify', status: 'ready', description: 'Compare SHA-256 against package metadata or lockfile.' },
    { id: 'signature', name: 'Signature verify', status: 'ready', description: 'Check publisher signature and trusted key policy.' },
    { id: 'scan', name: 'Scan', status: 'waiting', description: 'Run malware scanners, manifest validation, and risk scoring.' },
    { id: 'dependencies', name: 'Dependency resolve', status: 'ready', description: 'Resolve required, optional, peer, SDK, and game constraints.' },
    { id: 'permissions', name: 'Permissions approval', status: 'requires-approval', description: 'Show permission diff and require consent for new scopes.' },
    { id: 'install', name: 'Atomic install', status: 'waiting', description: 'Promote staged files into active mods directory.' },
    { id: 'rollback', name: 'Rollback point', status: 'ready', description: 'Record previous version and lockfile for one-click restore.' },
  ];

  try {
    const response = await fetch(`${apiBase}/install-pipeline`);
    if (!response.ok) {
      throw new Error(`Registry returned ${response.status}`);
    }
    return await response.json() as InstallPipelineStep[];
  } catch {
    return fallback;
  }
}

export async function fetchInstallQueue(): Promise<InstallQueueItem[]> {
  const fallback: InstallQueueItem[] = [
    {
      id: 'install-hello-world',
      packageId: 'hello-world',
      version: '1.0.0',
      status: 'ready-to-install',
      currentStage: 'permissions',
      progress: 72,
      rollback: 'available',
      createdAt: new Date().toISOString(),
    },
  ];

  try {
    const response = await fetch(`${apiBase}/install-queue`);
    if (!response.ok) {
      throw new Error(`Registry returned ${response.status}`);
    }
    return await response.json() as InstallQueueItem[];
  } catch {
    return fallback;
  }
}

export async function fetchDependencyGraph(): Promise<DependencyGraph> {
  const fallback: DependencyGraph = {
    nodes: [
      { id: 'hello-world', label: 'Hello World', kind: 'mod' },
      { id: 'shared-ui-core', label: 'Shared UI Core', kind: 'dependency' },
      { id: 'better-ui', label: 'Better UI', kind: 'mod' },
      { id: 'bridge-ui', label: 'UI Bridge Patch', kind: 'compatibility-patch' },
    ],
    edges: [
      { from: 'hello-world', to: 'shared-ui-core', label: 'requires' },
      { from: 'better-ui', to: 'shared-ui-core', label: 'requires' },
      { from: 'bridge-ui', to: 'hello-world', label: 'patches' },
      { from: 'bridge-ui', to: 'better-ui', label: 'patches' },
    ],
  };

  try {
    const response = await fetch(`${apiBase}/dependency-graph`);
    if (!response.ok) {
      throw new Error(`Registry returned ${response.status}`);
    }
    return await response.json() as DependencyGraph;
  } catch {
    return fallback;
  }
}

export async function fetchDocumentationLinks(): Promise<DocumentationLink[]> {
  const fallback: DocumentationLink[] = [
    { title: 'SDK Docs', href: '/SDK.md', description: 'Stable mod interfaces, lifecycle hooks, and services.' },
    { title: 'Manifest Schema', href: '/schemas/mod.schema.json', description: 'JSON Schema for mod.json validation and editor completion.' },
    { title: 'Packaging Guide', href: '/CLI.md', description: 'Validate, package, sign, publish, and diagnose mods.' },
    { title: 'Security Guide', href: '/SECURITY.md', description: 'Trust, signatures, permissions, quarantine, and safe mode.' },
    { title: 'Sample Mods', href: '/examples/README.md', description: 'Menu items, settings, events, themes, panels, permissions, and assets.' },
  ];

  try {
    const response = await fetch(`${apiBase}/docs-hub`);
    if (!response.ok) {
      throw new Error(`Registry returned ${response.status}`);
    }
    return await response.json() as DocumentationLink[];
  } catch {
    return fallback;
  }
}

export async function fetchAccountSecurity(token: string): Promise<AccountSecurityState> {
  try {
    const response = await fetch(`${apiBase}/account/security`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    if (!response.ok) {
      throw new Error(`Registry returned ${response.status}`);
    }
    return await response.json() as AccountSecurityState;
  } catch {
    return {
      emailVerified: false,
      trustedDevices: ['Current browser session'],
      sessions: [
        {
          id: 'local-fallback',
          createdAt: new Date().toISOString(),
          expiresAt: new Date(Date.now() + 86400000).toISOString(),
          revoked: false,
        },
      ],
      loginAudit: [],
    };
  }
}

export async function requestEmailVerification(token: string): Promise<{ ok: boolean; message?: string; devVerificationToken?: string }> {
  const response = await fetch(`${apiBase}/auth/email-verification/request`, {
    method: 'POST',
    headers: { Authorization: `Bearer ${token}` },
  });
  if (!response.ok) {
    throw new Error(await errorMessage(response));
  }
  return await response.json();
}

export async function requestPasswordReset(email: string): Promise<{ ok: boolean; message?: string; devResetToken?: string }> {
  const response = await fetch(`${apiBase}/auth/password-reset/request`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email }),
  });
  if (!response.ok) {
    throw new Error(await errorMessage(response));
  }
  return await response.json();
}

export async function fetchCollections(): Promise<MarketplaceCollection[]> {
  const fallback: MarketplaceCollection[] = [
    {
      id: 'unity-starter',
      name: 'Unity Starter Kit',
      curator: 'TheUnlocker Editorial',
      description: 'A conservative pack for first-time Unity mod users.',
      modIds: ['hello-world', 'shared-ui-core'],
      badges: ['Editor Pick', 'Signed Only', 'Low Risk'],
    },
  ];

  try {
    const response = await fetch(`${apiBase}/marketplace/collections`);
    if (!response.ok) {
      throw new Error(`Registry returned ${response.status}`);
    }
    return await response.json() as MarketplaceCollection[];
  } catch {
    return fallback;
  }
}

export async function fetchCompatibilitySignals(): Promise<CompatibilitySignal[]> {
  const fallback: CompatibilitySignal[] = [
    {
      modA: 'hello-world',
      modB: 'better-ui',
      installCount: 1842,
      crashCount: 3,
      risk: 'low',
      recommendation: 'Safe together. Load shared-ui-core before both mods.',
    },
  ];

  try {
    const response = await fetch(`${apiBase}/compatibility/signals`);
    if (!response.ok) {
      throw new Error(`Registry returned ${response.status}`);
    }
    return await response.json() as CompatibilitySignal[];
  } catch {
    return fallback;
  }
}

export async function fetchCompatibilityLab(): Promise<CompatibilityLabState> {
  const fallback: CompatibilityLabState = {
    activeJobs: 3,
    queuedJobs: 8,
    failedJobs: 1,
    passRate: '91.4%',
    adaptersCovered: 3,
    lastRunAt: new Date(Date.now() - 1080000).toISOString(),
    queue: {
      queued: 8,
      running: 3,
      failed: 1,
      deadLetter: 0,
      averageSeconds: 142,
    },
    adapters: [
      {
        id: 'unity',
        name: 'Unity Adapter',
        status: 'healthy',
        supportedVersions: ['2021 LTS', '2022 LTS', 'Unity 6'],
        lastTestAt: new Date(Date.now() - 1320000).toISOString(),
      },
      {
        id: 'unreal',
        name: 'Unreal Adapter',
        status: 'watching',
        supportedVersions: ['UE 5.3', 'UE 5.4'],
        lastTestAt: new Date(Date.now() - 7200000).toISOString(),
      },
      {
        id: 'minecraft',
        name: 'Minecraft Adapter',
        status: 'healthy',
        supportedVersions: ['Fabric 1.20', 'NeoForge 1.21'],
        lastTestAt: new Date(Date.now() - 2820000).toISOString(),
      },
    ],
    jobs: [
      {
        id: 'lab-unity-vanilla-plus',
        modpackId: 'vanilla-plus',
        gameId: 'unity',
        adapter: 'unity',
        status: 'completed',
        result: 'passed',
        durationSeconds: 118,
        startedAt: new Date(Date.now() - 1860000).toISOString(),
        findings: ['No launch crash', 'All declared dependencies satisfied', 'FPS delta within budget'],
        crashSignature: '',
        recommendation: 'Safe for stable ring.',
      },
      {
        id: 'lab-mc-experimental-physics',
        modpackId: 'experimental-physics-pack',
        gameId: 'minecraft',
        adapter: 'minecraft',
        status: 'completed',
        result: 'failed',
        durationSeconds: 52,
        startedAt: new Date(Date.now() - 3600000).toISOString(),
        findings: ['Launch crash reproduced', 'Conflict pair matched compatibility signals', 'Bridge patch available'],
        crashSignature: 'NullReference: legacy-save-tools SavePatch.Apply',
        recommendation: 'Recommend bridge-ui-save patch or disable legacy-save-tools.',
      },
    ],
  };

  try {
    const response = await fetch(`${apiBase}/compatibility/lab`);
    if (!response.ok) {
      throw new Error(`Registry returned ${response.status}`);
    }
    return await response.json() as CompatibilityLabState;
  } catch {
    return fallback;
  }
}

export async function fetchBuildFarmJobs(): Promise<BuildFarmState> {
  const fallback: BuildFarmState = {
    activeJobs: 4,
    queuedJobs: 12,
    successfulToday: 38,
    failedToday: 2,
    averageBuildTime: '3m 42s',
    lastCompletedAt: new Date(Date.now() - 540000).toISOString(),
    workers: [
      {
        id: 'worker-win-signing-01',
        pool: 'windows-signing',
        status: 'healthy',
        currentJob: 'build-better-ui-1-4-0',
        capabilities: ['dotnet', 'signing', 'sbom', 'yara'],
        lastHeartbeatAt: new Date(Date.now() - 28000).toISOString(),
      },
      {
        id: 'worker-linux-repro-02',
        pool: 'linux-reproducible',
        status: 'healthy',
        currentJob: 'build-shared-ui-core-2-1-0',
        capabilities: ['go', 'rust', 'cyclonedx', 'clamav'],
        lastHeartbeatAt: new Date(Date.now() - 44000).toISOString(),
      },
    ],
    jobs: [
      {
        id: 'build-better-ui-1-4-0',
        packageId: 'better-ui',
        version: '1.4.0',
        publisher: 'Sample Author',
        sourceCommit: '8f31c1b',
        ciRunUrl: 'https://ci.example/runs/8841',
        status: 'running',
        stage: 'signature',
        durationSeconds: 183,
        reproducible: true,
        sbomGenerated: true,
        signatureVerified: true,
        malwareScan: 'clean',
        provenanceAttested: true,
        artifactSha256: 'sha256-demo-better-ui-1-4-0',
        promotionRing: 'beta',
        startedAt: new Date(Date.now() - 240000).toISOString(),
        releaseRecommendation: 'Promote to beta after compatibility lab completes.',
      },
      {
        id: 'build-debug-tools-0-9-0',
        packageId: 'debug-tools',
        version: '0.9.0',
        publisher: 'local-dev',
        sourceCommit: 'local-dev',
        ciRunUrl: '',
        status: 'failed',
        stage: 'malware-scan',
        durationSeconds: 71,
        reproducible: false,
        sbomGenerated: true,
        signatureVerified: false,
        malwareScan: 'suspicious-imports',
        provenanceAttested: false,
        artifactSha256: 'sha256-demo-debug-tools-0-9-0',
        promotionRing: 'blocked',
        startedAt: new Date(Date.now() - 2220000).toISOString(),
        releaseRecommendation: 'Keep quarantined until publisher signs package and scan flags are reviewed.',
      },
    ],
  };

  try {
    const response = await fetch(`${apiBase}/build-farm/jobs`);
    if (!response.ok) {
      throw new Error(`Registry returned ${response.status}`);
    }
    return await response.json() as BuildFarmState;
  } catch {
    return fallback;
  }
}

export async function fetchRegistryFederation(): Promise<RegistryFederationState> {
  const fallback: RegistryFederationState = {
    query: 'ui',
    connected: 4,
    healthy: 3,
    blockedResults: 2,
    lastFederatedAt: new Date(Date.now() - 360000).toISOString(),
    policyVersion: 'team-policy-2026.05',
    defaultTrustLevel: 'TrustedPublisher',
    registries: [
      {
        id: 'official',
        name: 'Official Registry',
        url: 'https://registry.theunlocker.dev',
        kind: 'official',
        status: 'healthy',
        trustPolicy: 'Official signed publishers only',
        priority: 100,
        allowUnsigned: false,
        packagesIndexed: 1842,
        latencyMs: 42,
        lastSyncAt: new Date(Date.now() - 240000).toISOString(),
      },
      {
        id: 'studio-private',
        name: 'Studio Private',
        url: 'https://mods.studio.local',
        kind: 'private',
        status: 'healthy',
        trustPolicy: 'Allowed publishers: studio-official, tools-team',
        priority: 80,
        allowUnsigned: false,
        packagesIndexed: 128,
        latencyMs: 19,
        lastSyncAt: new Date(Date.now() - 540000).toISOString(),
      },
      {
        id: 'local-dev',
        name: 'Local Developer',
        url: 'http://localhost:4567',
        kind: 'local',
        status: 'healthy',
        trustPolicy: 'Local developer packages require explicit approval',
        priority: 30,
        allowUnsigned: true,
        packagesIndexed: 17,
        latencyMs: 3,
        lastSyncAt: new Date(Date.now() - 60000).toISOString(),
      },
      {
        id: 'community-unity',
        name: 'Unity Community',
        url: 'https://unity-mods.example',
        kind: 'community',
        status: 'degraded',
        trustPolicy: 'Unsigned packages hidden by default',
        priority: 50,
        allowUnsigned: false,
        packagesIndexed: 947,
        latencyMs: 211,
        lastSyncAt: new Date(Date.now() - 1860000).toISOString(),
      },
    ],
    results: [
      { packageId: 'better-ui', name: 'Better UI', version: '1.4.0', registryId: 'official', publisher: 'Sample Author', trustLevel: 'TrustedPublisher', allowedByPolicy: true, policyDecision: 'Signed trusted publisher. Compatible with active policy.', score: 98 },
      { packageId: 'shared-ui-core', name: 'Shared UI Core', version: '2.1.0', registryId: 'studio-private', publisher: 'studio-official', trustLevel: 'Official', allowedByPolicy: true, policyDecision: 'Publisher is allowlisted for private registry.', score: 96 },
      { packageId: 'debug-ui-tools', name: 'Debug UI Tools', version: '0.9.0', registryId: 'local-dev', publisher: 'local-dev', trustLevel: 'LocalDeveloper', allowedByPolicy: false, policyDecision: 'Requires explicit local developer approval before install.', score: 51 },
      { packageId: 'unsigned-ui-pack', name: 'Unsigned UI Pack', version: '1.0.0', registryId: 'community-unity', publisher: 'unknown', trustLevel: 'Unknown', allowedByPolicy: false, policyDecision: 'Unsigned community package hidden by current policy.', score: 22 },
    ],
  };

  try {
    const response = await fetch(`${apiBase}/registries/federation`);
    if (!response.ok) {
      throw new Error(`Registry returned ${response.status}`);
    }
    return await response.json() as RegistryFederationState;
  } catch {
    return fallback;
  }
}

export async function fetchTrustReputation(): Promise<TrustReputationState> {
  const fallback: TrustReputationState = {
    policyVersion: 'team-policy-2026.05',
    requiredTrust: 'TrustedPublisher',
    averageScore: 82,
    flaggedPackages: 2,
    trustedPublishers: 12,
    lastEvaluatedAt: new Date(Date.now() - 180000).toISOString(),
    packages: [
      {
        packageId: 'better-ui',
        name: 'Better UI',
        publisher: 'Sample Author',
        version: '1.4.0',
        score: 96,
        trustLevel: 'TrustedPublisher',
        decision: 'allow',
        recommendation: 'Safe for stable profiles.',
        positiveFactors: ['Publisher signature verified', 'SBOM generated by build farm', 'No active advisories', 'Low crash rate'],
        riskFactors: [],
      },
      {
        packageId: 'debug-tools',
        name: 'Debug Tools',
        publisher: 'local-dev',
        version: '0.9.0',
        score: 42,
        trustLevel: 'LocalDeveloper',
        decision: 'review',
        recommendation: 'Require local developer approval and keep out of stable profiles.',
        positiveFactors: ['Manifest validates', 'SBOM generated'],
        riskFactors: ['Unsigned binary', 'Network permission requested', 'Suspicious imports found during scan'],
      },
      {
        packageId: 'unsigned-ui-pack',
        name: 'Unsigned UI Pack',
        publisher: 'unknown',
        version: '1.0.0',
        score: 18,
        trustLevel: 'Unknown',
        decision: 'quarantine',
        recommendation: 'Quarantine until signature, publisher identity, and scan results are available.',
        positiveFactors: [],
        riskFactors: ['Unknown publisher', 'Unsigned package', 'No provenance attestation', 'No compatibility results'],
      },
    ],
    publishers: [
      { publisherId: 'sample-author', displayName: 'Sample Author', trustLevel: 'TrustedPublisher', verified: true, signedReleases: 18, activeAdvisories: 0, crashRate: '0.2%', reputationScore: 94 },
      { publisherId: 'studio-official', displayName: 'Studio Official', trustLevel: 'Official', verified: true, signedReleases: 44, activeAdvisories: 0, crashRate: '0.1%', reputationScore: 99 },
      { publisherId: 'local-dev', displayName: 'Local Developer', trustLevel: 'LocalDeveloper', verified: false, signedReleases: 0, activeAdvisories: 1, crashRate: 'unknown', reputationScore: 47 },
    ],
    advisories: [
      { id: 'ADV-2026-0007', packageId: 'debug-tools', severity: 'medium', status: 'open', summary: 'Network permission added without publisher signature.', publishedAt: new Date(Date.now() - 7200000).toISOString() },
      { id: 'ADV-2026-0004', packageId: 'legacy-save-tools', severity: 'high', status: 'mitigated', summary: 'Known crash with experimental-physics; bridge patch available.', publishedAt: new Date(Date.now() - 129600000).toISOString() },
    ],
  };

  try {
    const response = await fetch(`${apiBase}/trust/reputation`);
    if (!response.ok) {
      throw new Error(`Registry returned ${response.status}`);
    }
    return await response.json() as TrustReputationState;
  } catch {
    return fallback;
  }
}

export async function fetchCloudModpacks(): Promise<CloudModpackState> {
  const fallback: CloudModpackState = {
    featuredCount: 3,
    sharedInstalls: 18420,
    immutableLockfiles: 3,
    lastIndexedAt: new Date(Date.now() - 240000).toISOString(),
    modpacks: [
      {
        id: 'vanilla-plus',
        name: 'Vanilla+',
        version: '2.3.0',
        maintainers: ['TheUnlocker Editorial', 'Sample Author'],
        description: 'A stable curated Unity starter pack with exact versions, signed packages, and compatibility-lab approval.',
        lockfileUrl: 'https://registry.theunlocker.dev/modpacks/vanilla-plus/2.3.0/unlocker.lock.json',
        lockfileSha256: 'sha256-demo-vanilla-plus-lock',
        installUrl: 'theunlocker://install-pack/vanilla-plus',
        compatibility: 'verified',
        trustDecision: 'allow',
        modCount: 12,
        downloadSizeMb: 148,
        rollbackVersion: '2.2.1',
        updateRing: 'stable',
        lastCompatibilityAt: new Date(Date.now() - 1320000).toISOString(),
        badges: ['Signed Only', 'Low Risk', 'Compatibility Lab Passed'],
      },
      {
        id: 'creator-lab',
        name: 'Creator Lab',
        version: '0.8.0',
        maintainers: ['SDK Team'],
        description: 'A beta mod author pack for local developer mode, hot reload, panel samples, and SDK diagnostics.',
        lockfileUrl: 'https://registry.theunlocker.dev/modpacks/creator-lab/0.8.0/unlocker.lock.json',
        lockfileSha256: 'sha256-demo-creator-lab-lock',
        installUrl: 'theunlocker://install-pack/creator-lab',
        compatibility: 'watching',
        trustDecision: 'review',
        modCount: 18,
        downloadSizeMb: 224,
        rollbackVersion: '0.7.2',
        updateRing: 'beta',
        lastCompatibilityAt: new Date(Date.now() - 5400000).toISOString(),
        badges: ['Developer', 'Hot Reload', 'Beta Ring'],
      },
      {
        id: 'experimental-physics-pack',
        name: 'Experimental Physics Pack',
        version: '1.1.0',
        maintainers: ['Community Physics Group'],
        description: 'A high-risk nightly pack that requires a bridge patch before stable users should install it.',
        lockfileUrl: 'https://registry.theunlocker.dev/modpacks/experimental-physics-pack/1.1.0/unlocker.lock.json',
        lockfileSha256: 'sha256-demo-experimental-physics-lock',
        installUrl: 'theunlocker://install-pack/experimental-physics-pack',
        compatibility: 'failed',
        trustDecision: 'block',
        modCount: 9,
        downloadSizeMb: 316,
        rollbackVersion: '1.0.4',
        updateRing: 'nightly',
        lastCompatibilityAt: new Date(Date.now() - 3600000).toISOString(),
        badges: ['Bridge Patch Required', 'Nightly', 'Lab Failed'],
      },
    ],
  };

  try {
    const response = await fetch(`${apiBase}/modpacks/cloud`);
    if (!response.ok) {
      throw new Error(`Registry returned ${response.status}`);
    }
    return await response.json() as CloudModpackState;
  } catch {
    return fallback;
  }
}

export async function fetchAICompatibilityAssistant(): Promise<AICompatibilityAssistantState> {
  const fallback: AICompatibilityAssistantState = {
    analysisId: 'ai-compat-vanilla-plus-2026-05',
    subject: 'Vanilla+ profile with Better UI and Experimental Physics Pack',
    confidence: 'high',
    overallRisk: 'medium',
    generatedAt: new Date().toISOString(),
    recommendedAction: 'Install bridge-ui-save before enabling Experimental Physics Pack, then keep the pack in beta profile until the lab passes.',
    summary: 'The selected profile is mostly safe, but one nightly modpack has a repeated crash signature and a missing bridge patch.',
    suggestions: [
      {
        id: 'load-order-shared-ui',
        kind: 'load-order',
        severity: 'info',
        title: 'Load shared UI services first',
        detail: 'Place shared-ui-core before hello-world and better-ui so both mods bind the same SDK menu service.',
        affectedIds: ['shared-ui-core', 'hello-world', 'better-ui'],
        actions: ['Move shared-ui-core before UI mods', 'Regenerate lockfile', 'Run compatibility lab'],
      },
      {
        id: 'bridge-experimental-physics',
        kind: 'bridge-patch',
        severity: 'warning',
        title: 'Bridge patch recommended',
        detail: 'experimental-physics-pack and legacy-save-tools share a crash signature that is mitigated by bridge-ui-save.',
        affectedIds: ['experimental-physics-pack', 'legacy-save-tools', 'bridge-ui-save'],
        actions: ['Install bridge-ui-save', 'Keep pack in beta profile', 'Block nightly auto-update'],
      },
      {
        id: 'network-permission-review',
        kind: 'permission',
        severity: 'warning',
        title: 'Review new network permission',
        detail: 'debug-tools requests NetworkAccess while remaining unsigned. Require explicit local developer approval.',
        affectedIds: ['debug-tools'],
        actions: ['Keep disabled in stable profile', 'Ask publisher to sign package', 'Record permission approval decision'],
      },
    ],
    evidence: [
      { source: 'compatibility-lab', signal: 'experimental-physics-pack failed with legacy-save-tools crash signature' },
      { source: 'dependency-graph', signal: 'shared-ui-core is required by two active UI mods' },
      { source: 'trust-reputation', signal: 'debug-tools is unsigned and requests network access' },
    ],
  };

  try {
    const response = await fetch(`${apiBase}/ai/compatibility`);
    if (!response.ok) {
      throw new Error(`Registry returned ${response.status}`);
    }
    return await response.json() as AICompatibilityAssistantState;
  } catch {
    return fallback;
  }
}

export async function fetchPackageDiff(): Promise<PackageDiffState> {
  const fallback: PackageDiffState = {
    packageId: 'better-ui',
    name: 'Better UI',
    fromVersion: '1.3.1',
    toVersion: '1.4.0',
    publisher: 'Sample Author',
    generatedAt: new Date().toISOString(),
    riskChange: 'low-to-medium',
    decision: 'requires-approval',
    summary: 'The update is signed and compatible, but adds a menu permission and a settings migration.',
    permissionChanges: [
      { permission: 'AddMenuItems', change: 'added', reason: 'New command palette entry and Tools menu item' },
      { permission: 'SendNotifications', change: 'unchanged', reason: 'Existing update notification service' },
    ],
    dependencyChanges: [
      { dependency: 'shared-ui-core', from: '>=2.0.0 <3.0.0', to: '>=2.1.0 <3.0.0', change: 'tightened' },
      { dependency: 'theme-bridge', from: '', to: '>=1.0.0 <2.0.0', change: 'added-optional' },
    ],
    fileChanges: [
      { path: 'BetterUi.dll', change: 'modified', sha256: 'sha256-demo-better-ui-1-4-0', sizeDeltaKb: 42 },
      { path: 'assets/menu-icons.zip', change: 'added', sha256: 'sha256-demo-menu-icons', sizeDeltaKb: 188 },
      { path: 'mod.json', change: 'modified', sha256: 'sha256-demo-manifest', sizeDeltaKb: 1 },
    ],
    settingsMigrations: [
      { id: 'better-ui-settings-1-4', from: '1.3.x', to: '1.4.0', status: 'dry-run-ready', description: 'Moves toolbar visibility settings into the new menu service namespace.' },
    ],
    changelog: ['Adds command palette integration.', 'Adds optional theme bridge integration.', 'Moves toolbar settings to the shared menu namespace.'],
    rollback: {
      availableVersion: '1.3.1',
      snapshotId: 'rollback-better-ui-1-3-1',
      strategy: 'restore previous DLL, manifest, assets, and settings backup',
    },
  };

  try {
    const response = await fetch(`${apiBase}/packages/diff?packageId=better-ui&from=1.3.1&to=1.4.0`);
    if (!response.ok) {
      throw new Error(`Registry returned ${response.status}`);
    }
    return await response.json() as PackageDiffState;
  } catch {
    return fallback;
  }
}

export async function fetchPublisherDashboard(token: string): Promise<PublisherDashboard> {
  try {
    const response = await fetch(`${apiBase}/publishers/dashboard`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    if (!response.ok) {
      throw new Error(`Registry returned ${response.status}`);
    }
    return await response.json() as PublisherDashboard;
  } catch {
    return {
      publisherId: 'sample-author',
      displayName: 'Sample Author',
      verified: true,
      mods: 4,
      pendingUploads: 2,
      openCrashReports: 1,
      monthlyInstalls: 12840,
      conversionRate: '18.4%',
      averageRating: 4.8,
      signingKeys: ['publisher-key-demo-ed25519'],
      moderationStates: ['hello-world approved', 'better-ui scan-pending'],
      analyticsSegments: ['marketplace page views', 'install deep-link clicks', 'update adoption'],
    };
  }
}

export async function fetchPublisherAnalytics(token: string): Promise<PublisherAnalyticsState> {
  try {
    const response = await fetch(`${apiBase}/publishers/analytics`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    if (!response.ok) {
      throw new Error(`Registry returned ${response.status}`);
    }
    return await response.json() as PublisherAnalyticsState;
  } catch {
    const now = new Date();
    return {
      publisherId: 'sample-author',
      displayName: 'Sample Author',
      period: 'last-30-days',
      generatedAt: now.toISOString(),
      installs: 12840,
      updates: 9430,
      marketplaceViews: 69810,
      installClicks: 12840,
      conversionRate: '18.4%',
      averageRating: 4.8,
      crashRate: '0.2%',
      revenueEstimate: '$0.00',
      trend: [6, 5, 4, 3, 2, 1].map((daysAgo, index) => ({
        date: new Date(Date.now() - daysAgo * 86400000).toISOString().slice(0, 10),
        installs: 1420 + index * 154,
        updates: 990 + index * 132,
        crashes: 3 + (index % 3),
        views: 8120 + index * 760,
      })),
      topMods: [
        { modId: 'better-ui', name: 'Better UI', installs: 5820, updates: 4210, rating: 4.9, crashRate: '0.1%', conversionRate: '22.1%' },
        { modId: 'hello-world', name: 'Hello World', installs: 3860, updates: 2840, rating: 4.8, crashRate: '0.0%', conversionRate: '19.4%' },
        { modId: 'theme-bridge', name: 'Theme Bridge', installs: 1960, updates: 1475, rating: 4.6, crashRate: '0.3%', conversionRate: '14.7%' },
      ],
      funnel: [
        { stage: 'Marketplace views', count: 69810, rate: '100%' },
        { stage: 'Detail page opens', count: 31840, rate: '45.6%' },
        { stage: 'Install clicks', count: 12840, rate: '18.4%' },
        { stage: 'Completed installs', count: 12190, rate: '17.5%' },
        { stage: 'Enabled after install', count: 10940, rate: '15.7%' },
      ],
      adoption: [
        { version: '1.4.0', ring: 'stable', users: 7210, percentage: '56.2%' },
        { version: '1.3.1', ring: 'stable', users: 4380, percentage: '34.1%' },
        { version: '1.5.0-beta.1', ring: 'beta', users: 830, percentage: '6.5%' },
        { version: 'older', ring: 'legacy', users: 420, percentage: '3.2%' },
      ],
      moderationOutcomes: [
        { status: 'approved', count: 18, averageHours: 2.4 },
        { status: 'scan-pending', count: 2, averageHours: 0.6 },
        { status: 'needs-review', count: 1, averageHours: 7.8 },
      ],
    };
  }
}

export async function fetchRecoveryPlan(): Promise<RecoveryStep[]> {
  const fallback: RecoveryStep[] = [
    { id: 'safe-mode', label: 'Start safe mode', description: 'Disable all mods for the next launch while preserving profiles.' },
    { id: 'rollback', label: 'Rollback updates', description: 'Restore the last healthy package versions from rollback history.' },
  ];

  try {
    const response = await fetch(`${apiBase}/recovery/plan`);
    if (!response.ok) {
      throw new Error(`Registry returned ${response.status}`);
    }
    return await response.json() as RecoveryStep[];
  } catch {
    return fallback;
  }
}

export async function fetchDesktopReleases(): Promise<DesktopReleaseState> {
  const fallback: DesktopReleaseState = {
    currentVersion: '1.1.2',
    activeChannel: 'stable',
    autoUpdate: true,
    lastCheckedAt: new Date().toISOString(),
    policy: {
      requireSignedUpdates: true,
      allowPrerelease: false,
      rollbackOnFailure: true,
      allowedChannels: ['stable', 'beta'],
    },
    releases: [
      {
        version: '1.1.2',
        channel: 'stable',
        downloadUrl: '#',
        sha256: 'demo-stable-sha256',
        signatureUrl: '#',
        changelog: 'Adds release center, safer update policy display, and rollback health checks.',
        health: 'healthy',
        signed: true,
        rolloutPercentage: 100,
        publishedAt: new Date().toISOString(),
      },
      {
        version: '1.2.0-beta.1',
        channel: 'beta',
        downloadUrl: '#',
        sha256: 'demo-beta-sha256',
        signatureUrl: '#',
        changelog: 'Preview build for remote orchestration and cloud profile sync refinements.',
        health: 'watching',
        signed: true,
        rolloutPercentage: 25,
        publishedAt: new Date().toISOString(),
      },
      {
        version: '1.3.0-nightly.42',
        channel: 'nightly',
        downloadUrl: '#',
        sha256: 'demo-nightly-sha256',
        signatureUrl: '#',
        changelog: 'Nightly automation and marketplace experiment channel.',
        health: 'canary',
        signed: true,
        rolloutPercentage: 5,
        publishedAt: new Date().toISOString(),
      },
    ],
    rollback: {
      availableVersion: '1.0.0',
      lastHealthyAt: new Date().toISOString(),
      plan: 'Install signed release, run launch health check, rollback to 1.0.0 if startup fails twice.',
    },
  };

  try {
    const response = await fetch(`${apiBase}/releases/desktop`);
    if (!response.ok) {
      throw new Error(`Registry returned ${response.status}`);
    }
    return await response.json() as DesktopReleaseState;
  } catch {
    return fallback;
  }
}

export async function fetchDeviceFleet(token: string): Promise<DeviceFleetState> {
  const fallback: DeviceFleetState = {
    accountId: 'demo-user',
    onlineDevices: 2,
    pendingCommands: 3,
    lastFleetSyncAt: new Date().toISOString(),
    orchestrationKey: 'demo-fleet-ed25519',
    devices: [
      {
        id: 'desktop-main',
        name: 'Main gaming PC',
        kind: 'WindowsDesktop',
        status: 'online',
        lastSeenAt: new Date(Date.now() - 120000).toISOString(),
        registryUrl: 'https://registry.theunlocker.local',
        activeProfile: 'Vanilla+',
        installedMods: 42,
        trustLevel: 'TrustedDevice',
        canReceiveCommands: true,
        lanAddress: '192.168.1.42',
        pendingCommands: [
          { id: 'cmd-install-better-ui', type: 'install', status: 'queued', target: 'better-ui@1.4.0' },
          { id: 'cmd-sync-policy', type: 'policy-sync', status: 'ready', target: 'enterprise-policy' },
        ],
      },
      {
        id: 'steamdeck-living-room',
        name: 'Living room handheld',
        kind: 'PortableClient',
        status: 'online',
        lastSeenAt: new Date(Date.now() - 480000).toISOString(),
        registryUrl: 'https://registry.theunlocker.local',
        activeProfile: 'Travel',
        installedMods: 18,
        trustLevel: 'TrustedDevice',
        canReceiveCommands: true,
        lanAddress: '192.168.1.73',
        pendingCommands: [
          { id: 'cmd-install-pack-vanilla-plus', type: 'install-pack', status: 'waiting-for-user', target: 'vanilla-plus' },
        ],
      },
      {
        id: 'lab-vm',
        name: 'Compatibility lab VM',
        kind: 'SandboxRunner',
        status: 'offline',
        lastSeenAt: new Date(Date.now() - 10800000).toISOString(),
        registryUrl: 'http://localhost:4567',
        activeProfile: 'Testing',
        installedMods: 7,
        trustLevel: 'LocalDeveloper',
        canReceiveCommands: false,
        lanAddress: '',
        pendingCommands: [],
      },
    ],
  };

  try {
    const response = await fetch(`${apiBase}/devices/fleet`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    if (!response.ok) {
      throw new Error(`Registry returned ${response.status}`);
    }
    return await response.json() as DeviceFleetState;
  } catch {
    return fallback;
  }
}

export async function fetchWorkflowRules(): Promise<WorkflowRule[]> {
  const fallback: WorkflowRule[] = [
    { id: 'backup-before-launch', trigger: 'before-launch', condition: 'profile has save-risk mods', action: 'backup save files', enabled: true },
  ];

  try {
    const response = await fetch(`${apiBase}/workflows/rules`);
    if (!response.ok) {
      throw new Error(`Registry returned ${response.status}`);
    }
    return await response.json() as WorkflowRule[];
  } catch {
    return fallback;
  }
}

export async function fetchModerationQueue(token: string): Promise<ModerationQueueItem[]> {
  const fallback: ModerationQueueItem[] = [
    {
      id: 'upload-debug-tools-0-9-0',
      packageId: 'debug-tools',
      publisher: 'local-dev',
      status: 'needs-review',
      riskScore: 71,
      flags: ['unsigned', 'network permission', 'packed binary'],
      submittedAt: new Date().toISOString(),
    },
  ];

  try {
    const response = await fetch(`${apiBase}/admin/moderation`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    if (!response.ok) {
      throw new Error(`Registry returned ${response.status}`);
    }
    return await response.json() as ModerationQueueItem[];
  } catch {
    return fallback;
  }
}

export async function fetchNotifications(token: string): Promise<PlatformNotification[]> {
  const fallback: PlatformNotification[] = [
    {
      id: 'permission-diff',
      severity: 'warning',
      title: 'Permission approval needed',
      body: 'Better UI requests a new permission.',
      createdAt: new Date().toISOString(),
    },
  ];

  try {
    const response = await fetch(`${apiBase}/notifications`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    if (!response.ok) {
      throw new Error(`Registry returned ${response.status}`);
    }
    return await response.json() as PlatformNotification[];
  } catch {
    return fallback;
  }
}

export async function fetchEffectivePolicy(token: string): Promise<EffectivePolicy> {
  try {
    const response = await fetch(`${apiBase}/policy/effective`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    if (!response.ok) {
      throw new Error(`Registry returned ${response.status}`);
    }
    return await response.json() as EffectivePolicy;
  } catch {
    return {
      source: 'local-fallback',
      version: 'dev',
      allowUnsignedMods: false,
      requiredTrustLevel: 'TrustedPublisher',
      allowedRegistries: ['official', 'local-dev'],
      blockedPermissions: ['ArbitraryFileWrite'],
      blockedMods: [],
      lastSyncedAt: new Date().toISOString(),
      nextSyncRecommended: new Date(Date.now() + 21600000).toISOString(),
    };
  }
}

export async function fetchPolicySimulations(token: string): Promise<PolicySimulationState> {
  const fallback: PolicySimulationState = {
    policyVersion: '2026.05.04-enterprise',
    environment: 'local-fallback',
    generatedAt: new Date().toISOString(),
    overallDecision: 'review',
    allowedCount: 2,
    reviewCount: 1,
    blockedCount: 1,
    rules: [
      {
        id: 'signature-required',
        label: 'Require trusted signatures',
        enabled: true,
        severity: 'block',
        description: 'Unsigned packages from non-local registries are blocked before install.',
      },
      {
        id: 'network-consent',
        label: 'Network permission consent',
        enabled: true,
        severity: 'review',
        description: 'Network permissions require approval and audit logging.',
      },
    ],
    scenarios: [
      {
        id: 'better-ui-stable',
        title: 'Better UI stable update',
        packageId: 'better-ui',
        version: '1.4.0',
        registry: 'official',
        trustLevel: 'TrustedPublisher',
        requestedPermissions: ['AddMenuItems', 'SendNotifications'],
        updateRing: 'stable',
        decision: 'allow',
        score: 94,
        findings: [
          { ruleId: 'signature-required', severity: 'info', message: 'Publisher signature and public key chain verified.' },
        ],
      },
      {
        id: 'unsigned-ui-community',
        title: 'Unsigned community UI pack',
        packageId: 'unsigned-ui-pack',
        version: '1.0.0',
        registry: 'community-unity',
        trustLevel: 'Unknown',
        requestedPermissions: ['AddMenuItems'],
        updateRing: 'stable',
        decision: 'block',
        score: 18,
        findings: [
          { ruleId: 'signature-required', severity: 'critical', message: 'Package is unsigned and publisher identity is unknown.' },
        ],
      },
    ],
    recommendedActions: [
      'Approve stable signed updates automatically for trusted publishers.',
      'Keep unsigned community packages hidden until publisher verification is complete.',
    ],
  };

  try {
    const response = await fetch(`${apiBase}/policy/simulations`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    if (!response.ok) {
      throw new Error(`Registry returned ${response.status}`);
    }
    return await response.json() as PolicySimulationState;
  } catch {
    return fallback;
  }
}

export async function fetchRegistryHealthDetails(): Promise<RegistryServiceHealth[]> {
  const fallback: RegistryServiceHealth[] = [
    { service: 'go-api', status: 'healthy', latencyMs: 12, detail: 'public gateway responding' },
    { service: 'worker', status: 'healthy', latencyMs: 18, detail: 'last heartbeat received' },
  ];

  try {
    const response = await fetch(`${apiBase}/registry/health`);
    if (!response.ok) {
      throw new Error(`Registry returned ${response.status}`);
    }
    return await response.json() as RegistryServiceHealth[];
  } catch {
    return fallback;
  }
}

export async function fetchMod(id: string): Promise<RegistryMod> {
  try {
    const response = await fetch(`${apiBase}/mods/${encodeURIComponent(id)}`);
    if (!response.ok) {
      throw new Error(`Registry returned ${response.status}`);
    }
    return enrichMod(await response.json());
  } catch {
    return enrichMod(fallbackMods.find((mod) => mod.id === id) ?? fallbackMods[0]);
  }
}

export async function fetchHealth(): Promise<RegistryHealth> {
  try {
    const response = await fetch(`${apiBase}/health`);
    if (!response.ok) {
      throw new Error(`Health returned ${response.status}`);
    }
    return await response.json();
  } catch {
    return {
      status: 'Offline fallback',
      registry: 'unavailable',
      redis: 'unknown',
      mongo: 'unknown',
      minio: 'unknown',
      checkedAt: new Date().toISOString(),
    };
  }
}

export async function createAccount(displayName: string, email: string, password: string): Promise<AuthSession> {
  return postAuth(`${apiBase}/auth/register`, { displayName, email, password });
}

export async function signIn(email: string, password: string): Promise<AuthSession> {
  return postAuth(`${apiBase}/auth/login`, { email, password });
}

export async function fetchSession(token: string): Promise<AuthSession> {
  const response = await fetch(`${apiBase}/auth/session`, {
    headers: { Authorization: `Bearer ${token}` },
  });
  if (!response.ok) {
    throw new Error('Session expired');
  }
  return await response.json();
}

export async function completeOnboarding(
  token: string,
  payload: { role: string; primaryGame: string; registryUrl: string },
): Promise<AuthSession> {
  const response = await fetch(`${apiBase}/onboarding`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      Authorization: `Bearer ${token}`,
    },
    body: JSON.stringify(payload),
  });
  if (!response.ok) {
    throw new Error(await errorMessage(response));
  }
  return await response.json();
}

export async function updateAccountSettings(
  token: string,
  payload: { displayName: string; primaryGame: string; registryUrl: string; password?: string },
): Promise<AuthSession> {
  const response = await fetch(`${apiBase}/account/settings`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      Authorization: `Bearer ${token}`,
    },
    body: JSON.stringify(payload),
  });
  if (!response.ok) {
    throw new Error(await errorMessage(response));
  }
  return await response.json();
}

export async function signOut(token: string): Promise<void> {
  await fetch(`${apiBase}/auth/logout`, {
    method: 'POST',
    headers: { Authorization: `Bearer ${token}` },
  });
}

async function postAuth(route: string, payload: unknown): Promise<AuthSession> {
  const response = await fetch(route, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload),
  });
  if (!response.ok) {
    throw new Error(await errorMessage(response));
  }
  return await response.json();
}

async function errorMessage(response: Response): Promise<string> {
  try {
    const body = await response.json();
    return body.error ?? `Request failed with ${response.status}`;
  } catch {
    return `Request failed with ${response.status}`;
  }
}

function enrichMod(mod: RegistryMod): RegistryMod {
  return {
    ...mod,
    dependencies: mod.dependencies ?? ['shared-ui-core'],
    screenshots: mod.screenshots ?? ['/screenshots/marketplace.png', '/screenshots/platform.png'],
    rating: mod.rating ?? 4.8,
    installCount: mod.installCount ?? 12840,
    publisher: mod.publisher ?? {
      id: mod.author.toLowerCase().replaceAll(' ', '-'),
      name: mod.author,
      verified: mod.trustLevel === 'Official' || mod.trustLevel === 'Trusted Publisher',
      publicKey: 'publisher-key-demo-ed25519',
      trustHistory: ['Signed package', 'Passed malware scan', 'No active advisories'],
      stats: {
        mods: 4,
        installs: 52130,
        crashRate: '0.2%',
      },
    },
    reviews: mod.reviews ?? [
      {
        id: 'review-1',
        author: 'Local Tester',
        rating: 5,
        body: 'Installed cleanly and declared permissions clearly.',
        status: 'visible',
        publisherReply: 'Thanks for testing the signed build.',
      },
    ],
  };
}

function rankMods(mods: RegistryMod[], query: string): RegistryMod[] {
  const needle = query.trim().toLowerCase();
  return [...mods].sort((left, right) => scoreMod(right, needle) - scoreMod(left, needle));
}

function scoreMod(mod: RegistryMod, query: string): number {
  const exactMatch = query && [mod.id, mod.name].some((value) => value.toLowerCase().includes(query)) ? 20 : 0;
  const trust = mod.trustLevel === 'Official' ? 30 : mod.trustLevel === 'Trusted Publisher' ? 20 : 5;
  const rating = (mod.rating ?? 0) * 5;
  const installs = Math.min((mod.installCount ?? 0) / 1000, 20);
  const recency = mod.versions[0]?.createdAt ? 5 : 0;
  return exactMatch + trust + rating + installs + recency;
}
