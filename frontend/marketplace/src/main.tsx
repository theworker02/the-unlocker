import React, { FormEvent, useEffect, useMemo, useState } from 'react';
import { createRoot } from 'react-dom/client';
import { BrowserRouter, Navigate, Route, Routes, useNavigate, useParams } from 'react-router-dom';
import {
  Activity,
  BookOpen,
  Cable,
  Cpu,
  FileDiff,
  Download,
  Filter,
  FlaskConical,
  GitBranch,
  HeartPulse,
  LogOut,
  Rocket,
  Search,
  ShieldCheck,
  Sparkles,
  UploadCloud,
  UserRound,
  Wrench,
} from 'lucide-react';
import {
  completeOnboarding,
  createAccount,
  fetchAccountSecurity,
  fetchAICompatibilityAssistant,
  fetchBuildFarmJobs,
  fetchCloudModpacks,
  fetchCompatibilityLab,
  fetchCollections,
  fetchCompatibilitySignals,
  fetchDependencyGraph,
  fetchDocumentationLinks,
  fetchEffectivePolicy,
  fetchHealth,
  fetchInstallQueue,
  fetchInstallPipeline,
  fetchDesktopReleases,
  fetchDeviceFleet,
  fetchMod,
  fetchMods,
  fetchPackageDiff,
  fetchPolicySimulations,
  fetchPlatformUpgrades,
  fetchProductUpgrades,
  fetchPublisherAnalytics,
  fetchPublisherDashboard,
  fetchRecoveryPlan,
  fetchModerationQueue,
  fetchNotifications,
  fetchRegistryFederation,
  fetchRegistryHealthDetails,
  fetchSession,
  fetchTrustReputation,
  fetchWorkflowRules,
  requestEmailVerification,
  requestPasswordReset,
  signIn,
  signOut,
  updateAccountSettings,
} from './api';
import type {
  AccountSecurityState,
  AICompatibilityAssistantState,
  AuthMode,
  AuthSession,
  BuildFarmState,
  CloudModpackState,
  CompatibilityLabState,
  CompatibilitySignal,
  DependencyGraph,
  DocumentationLink,
  DesktopReleaseState,
  DeviceFleetState,
  EffectivePolicy,
  InstallPipelineStep,
  InstallQueueItem,
  MarketplaceCollection,
  MarketplacePage,
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
import './styles.css';

const sessionStorageKey = 'theunlocker.session';

function App({ routePage }: { routePage?: MarketplacePage }) {
  const navigate = useNavigate();
  const { modId } = useParams();
  const [mods, setMods] = useState<RegistryMod[]>([]);
  const [health, setHealth] = useState<RegistryHealth | null>(null);
  const [query, setQuery] = useState('');
  const [game, setGame] = useState('');
  const [trust, setTrust] = useState('');
  const [session, setSession] = useState<AuthSession | null>(null);
  const [authMode, setAuthMode] = useState<AuthMode>('signin');
  const [authError, setAuthError] = useState('');
  const [isRestoring, setIsRestoring] = useState(true);
  const [page, setPage] = useState<MarketplacePage>(routePage ?? 'mods');
  const [selectedMod, setSelectedMod] = useState<RegistryMod | null>(null);
  const [platformUpgrades, setPlatformUpgrades] = useState<PlatformUpgrade[]>([]);
  const [productUpgrades, setProductUpgrades] = useState<ProductUpgrade[]>([]);
  const [installPipeline, setInstallPipeline] = useState<InstallPipelineStep[]>([]);
  const [dependencyGraph, setDependencyGraph] = useState<DependencyGraph | null>(null);
  const [documentationLinks, setDocumentationLinks] = useState<DocumentationLink[]>([]);
  const [collections, setCollections] = useState<MarketplaceCollection[]>([]);
  const [compatibilitySignals, setCompatibilitySignals] = useState<CompatibilitySignal[]>([]);
  const [compatibilityLab, setCompatibilityLab] = useState<CompatibilityLabState | null>(null);
  const [buildFarm, setBuildFarm] = useState<BuildFarmState | null>(null);
  const [accountSecurity, setAccountSecurity] = useState<AccountSecurityState | null>(null);
  const [installQueue, setInstallQueue] = useState<InstallQueueItem[]>([]);
  const [publisherDashboard, setPublisherDashboard] = useState<PublisherDashboard | null>(null);
  const [publisherAnalytics, setPublisherAnalytics] = useState<PublisherAnalyticsState | null>(null);
  const [recoveryPlan, setRecoveryPlan] = useState<RecoveryStep[]>([]);
  const [workflowRules, setWorkflowRules] = useState<WorkflowRule[]>([]);
  const [moderationQueue, setModerationQueue] = useState<ModerationQueueItem[]>([]);
  const [notifications, setNotifications] = useState<PlatformNotification[]>([]);
  const [effectivePolicy, setEffectivePolicy] = useState<EffectivePolicy | null>(null);
  const [policySimulations, setPolicySimulations] = useState<PolicySimulationState | null>(null);
  const [registryHealthDetails, setRegistryHealthDetails] = useState<RegistryServiceHealth[]>([]);
  const [registryFederation, setRegistryFederation] = useState<RegistryFederationState | null>(null);
  const [trustReputation, setTrustReputation] = useState<TrustReputationState | null>(null);
  const [cloudModpacks, setCloudModpacks] = useState<CloudModpackState | null>(null);
  const [aiCompatibility, setAICompatibility] = useState<AICompatibilityAssistantState | null>(null);
  const [packageDiff, setPackageDiff] = useState<PackageDiffState | null>(null);
  const [desktopReleases, setDesktopReleases] = useState<DesktopReleaseState | null>(null);
  const [deviceFleet, setDeviceFleet] = useState<DeviceFleetState | null>(null);

  const searchParams = useMemo(() => {
    const params = new URLSearchParams();
    if (query.trim()) params.set('q', query.trim());
    if (game.trim()) params.set('game', game.trim());
    if (trust.trim()) params.set('trust', trust.trim());
    return params;
  }, [query, game, trust]);

  useEffect(() => {
    fetchMods(searchParams).then(setMods);
  }, [searchParams]);

  useEffect(() => {
    fetchHealth().then(setHealth);
    fetchPlatformUpgrades().then(setPlatformUpgrades);
    fetchProductUpgrades().then(setProductUpgrades);
    fetchInstallPipeline().then(setInstallPipeline);
    fetchDependencyGraph().then(setDependencyGraph);
    fetchDocumentationLinks().then(setDocumentationLinks);
    fetchCollections().then(setCollections);
    fetchCompatibilityLab().then(setCompatibilityLab);
    fetchCompatibilitySignals().then(setCompatibilitySignals);
    fetchBuildFarmJobs().then(setBuildFarm);
    fetchInstallQueue().then(setInstallQueue);
    fetchRecoveryPlan().then(setRecoveryPlan);
    fetchDesktopReleases().then(setDesktopReleases);
    fetchWorkflowRules().then(setWorkflowRules);
    fetchRegistryHealthDetails().then(setRegistryHealthDetails);
    fetchRegistryFederation().then(setRegistryFederation);
    fetchTrustReputation().then(setTrustReputation);
    fetchCloudModpacks().then(setCloudModpacks);
    fetchAICompatibilityAssistant().then(setAICompatibility);
    fetchPackageDiff().then(setPackageDiff);
  }, []);

  useEffect(() => {
    if (!session) {
      setAccountSecurity(null);
      setPolicySimulations(null);
      return;
    }
    fetchAccountSecurity(session.token).then(setAccountSecurity);
    fetchPublisherDashboard(session.token).then(setPublisherDashboard);
    fetchPublisherAnalytics(session.token).then(setPublisherAnalytics);
    fetchDeviceFleet(session.token).then(setDeviceFleet);
    fetchModerationQueue(session.token).then(setModerationQueue);
    fetchNotifications(session.token).then(setNotifications);
    fetchEffectivePolicy(session.token).then(setEffectivePolicy);
    fetchPolicySimulations(session.token).then(setPolicySimulations);
  }, [session]);

  useEffect(() => {
    setPage(routePage ?? 'mods');
  }, [routePage]);

  useEffect(() => {
    if (!modId) {
      setSelectedMod(null);
      return;
    }

    fetchMod(modId).then(setSelectedMod);
  }, [modId]);

  useEffect(() => {
    const saved = localStorage.getItem(sessionStorageKey);
    if (!saved) {
      setIsRestoring(false);
      return;
    }

    let parsed: AuthSession;
    try {
      parsed = JSON.parse(saved) as AuthSession;
    } catch {
      localStorage.removeItem(sessionStorageKey);
      setIsRestoring(false);
      return;
    }

    fetchSession(parsed.token)
      .then((restored) => {
        setSession(restored);
        localStorage.setItem(sessionStorageKey, JSON.stringify(restored));
      })
      .catch(() => localStorage.removeItem(sessionStorageKey))
      .finally(() => setIsRestoring(false));
  }, []);

  const persistSession = (nextSession: AuthSession) => {
    setSession(nextSession);
    localStorage.setItem(sessionStorageKey, JSON.stringify(nextSession));
  };

  const handleLogout = async () => {
    if (session) {
      await signOut(session.token);
    }
    localStorage.removeItem(sessionStorageKey);
    setSession(null);
    setAuthMode('signin');
  };

  const openModDetails = (id: string) => {
    navigate(`/mods/${encodeURIComponent(id)}`);
  };

  if (isRestoring) {
    return <Splash status="Restoring session..." />;
  }

  if (!session) {
    return (
      <AuthScreen
        mode={authMode}
        error={authError}
        setMode={setAuthMode}
        onSubmit={async (displayName, email, password) => {
          setAuthError('');
          try {
            if (authMode === 'reset') {
              const result = await requestPasswordReset(email);
              setAuthError(result.devResetToken ? `Reset requested. Dev token: ${result.devResetToken}` : 'Reset requested. Check your email.');
              return;
            }
            const nextSession = authMode === 'create'
              ? await createAccount(displayName, email, password)
              : await signIn(email, password);
            persistSession(nextSession);
          } catch (error) {
            setAuthError(error instanceof Error ? error.message : 'Authentication failed');
          }
        }}
      />
    );
  }

  if (!session.user.onboardingComplete) {
    return (
      <OnboardingScreen
        session={session}
        onComplete={async (payload) => {
          const nextSession = await completeOnboarding(session.token, payload);
          persistSession(nextSession);
        }}
        onLogout={handleLogout}
      />
    );
  }

  return (
    <main className="shell">
      <section className="masthead">
        <div>
          <p className="eyebrow">TheUnlocker Marketplace</p>
          <h1>Browse trusted mods and modpacks</h1>
          <p className="lede">
            Signed in as {session.user.displayName}. Your session and onboarding preferences persist across visits.
          </p>
        </div>
          <div className="masthead-actions">
            <div className="health">
              <HeartPulse size={18} />
              <span>{health?.status ?? 'Checking...'}</span>
            </div>
            <div className="nav-actions">
              <button className="ghost-button" type="button" onClick={() => navigate('/')}>Mods</button>
              <button className="ghost-button" type="button" onClick={() => navigate('/publishers/sample-author')}>Publishers</button>
              <button className="ghost-button" type="button" onClick={() => navigate('/publisher-analytics')}>Analytics</button>
              <button className="ghost-button" type="button" onClick={() => navigate('/modpacks/vanilla-plus')}>Modpacks</button>
              <button className="ghost-button" type="button" onClick={() => navigate('/cloud-modpacks')}>Cloud Packs</button>
              <button className="ghost-button" type="button" onClick={() => navigate('/collections')}>Collections</button>
              <button className="ghost-button" type="button" onClick={() => navigate('/compatibility')}>Compatibility</button>
              <button className="ghost-button" type="button" onClick={() => navigate('/assistant')}>Assistant</button>
              <button className="ghost-button" type="button" onClick={() => navigate('/package-diff')}>Diff</button>
              <button className="ghost-button" type="button" onClick={() => navigate('/lab')}>Lab</button>
              <button className="ghost-button" type="button" onClick={() => navigate('/builds')}>Builds</button>
              <button className="ghost-button" type="button" onClick={() => navigate('/federation')}>Federation</button>
              <button className="ghost-button" type="button" onClick={() => navigate('/trust')}>Trust</button>
              <button className="ghost-button" type="button" onClick={() => navigate('/policy-lab')}>Policy Lab</button>
              <button className="ghost-button" type="button" onClick={() => navigate('/operations')}>Operations</button>
              <button className="ghost-button" type="button" onClick={() => navigate('/control-center')}>Control</button>
              <button className="ghost-button" type="button" onClick={() => navigate('/devices')}>Devices</button>
              <button className="ghost-button" type="button" onClick={() => navigate('/releases')}>Releases</button>
              <button className="ghost-button" type="button" onClick={() => navigate('/governance')}>Governance</button>
              <button className="ghost-button" type="button" onClick={() => navigate('/platform')}>Platform</button>
              <button className="ghost-button" type="button" onClick={() => navigate('/docs-hub')}>Docs</button>
              <button className="ghost-button" type="button" onClick={() => navigate('/settings')}>Account</button>
            </div>
            <button className="ghost-button" type="button" onClick={handleLogout}>
              <LogOut size={16} />
              Sign out
            </button>
        </div>
      </section>

      {selectedMod ? (
        <ModDetail mod={selectedMod} onBack={() => navigate('/')} />
      ) : page === 'settings' ? (
        <AccountSettings
          session={session}
          security={accountSecurity}
          onVerifyEmail={async () => {
            const result = await requestEmailVerification(session.token);
            await fetchAccountSecurity(session.token).then(setAccountSecurity);
            return result.devVerificationToken ? `Verification requested. Dev token: ${result.devVerificationToken}` : 'Verification requested.';
          }}
          onSave={async (payload) => {
            const nextSession = await updateAccountSettings(session.token, payload);
            persistSession(nextSession);
          }}
        />
      ) : page === 'publisher' ? (
        <PublisherPage mods={mods} dashboard={publisherDashboard} />
      ) : page === 'analytics' ? (
        <PublisherAnalyticsPage analytics={publisherAnalytics} />
      ) : page === 'modpack' ? (
        <ModpackPage mods={mods} graph={dependencyGraph} />
      ) : page === 'cloudpacks' ? (
        <CloudModpackPage cloudModpacks={cloudModpacks} />
      ) : page === 'operations' ? (
        <OperationsPage upgrades={productUpgrades} pipeline={installPipeline} graph={dependencyGraph} />
      ) : page === 'collections' ? (
        <CollectionsPage collections={collections} />
      ) : page === 'compatibility' ? (
        <CompatibilityPage signals={compatibilitySignals} graph={dependencyGraph} />
      ) : page === 'assistant' ? (
        <AICompatibilityAssistantPage assistant={aiCompatibility} />
      ) : page === 'diff' ? (
        <PackageDiffPage diff={packageDiff} />
      ) : page === 'lab' ? (
        <CompatibilityLabPage lab={compatibilityLab} />
      ) : page === 'builds' ? (
        <BuildFarmPage buildFarm={buildFarm} />
      ) : page === 'federation' ? (
        <FederationPage federation={registryFederation} />
      ) : page === 'trust' ? (
        <TrustReputationPage trust={trustReputation} />
      ) : page === 'policylab' ? (
        <PolicySimulationPage simulations={policySimulations} />
      ) : page === 'control' ? (
        <ControlCenterPage queue={installQueue} recoveryPlan={recoveryPlan} workflowRules={workflowRules} />
      ) : page === 'devices' ? (
        <DeviceFleetPage fleet={deviceFleet} />
      ) : page === 'releases' ? (
        <ReleaseCenterPage releases={desktopReleases} />
      ) : page === 'governance' ? (
        <GovernancePage
          moderationQueue={moderationQueue}
          notifications={notifications}
          policy={effectivePolicy}
          health={registryHealthDetails}
        />
      ) : page === 'docs' ? (
        <DocumentationHub links={documentationLinks} />
      ) : page === 'platform' ? (
        <PlatformPage upgrades={platformUpgrades} productUpgrades={productUpgrades} />
      ) : (
        <MarketplaceList
          mods={mods}
          query={query}
          game={game}
          trust={trust}
          setQuery={setQuery}
          setGame={setGame}
          setTrust={setTrust}
          openModDetails={openModDetails}
        />
      )}
    </main>
  );
}

function Splash({ status }: { status: string }) {
  return (
    <main className="center-shell">
      <section className="auth-panel">
        <Sparkles size={28} />
        <h1>TheUnlocker</h1>
        <p>{status}</p>
      </section>
    </main>
  );
}

function AuthScreen({
  mode,
  error,
  setMode,
  onSubmit,
}: {
  mode: AuthMode;
  error: string;
  setMode: (mode: AuthMode) => void;
  onSubmit: (displayName: string, email: string, password: string) => Promise<void>;
}) {
  const [displayName, setDisplayName] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    setIsSubmitting(true);
    try {
      await onSubmit(displayName, email, password);
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <main className="center-shell">
      <section className="auth-panel">
        <div className="auth-icon"><UserRound size={24} /></div>
        <p className="eyebrow">Persistent sessions</p>
        <h1>{mode === 'create' ? 'Create your account' : mode === 'reset' ? 'Reset your password' : 'Welcome back'}</h1>
        <p className="panel-copy">
          Save profiles, registry preferences, installs, and onboarding choices across marketplace sessions.
        </p>
        <form className="auth-form" onSubmit={submit}>
          {mode === 'create' && (
            <label>
              Display name
              <input value={displayName} onChange={(event) => setDisplayName(event.target.value)} required />
            </label>
          )}
          <label>
            Email
            <input type="email" value={email} onChange={(event) => setEmail(event.target.value)} required />
          </label>
          {mode !== 'reset' && (
            <label>
              Password
              <input type="password" minLength={8} value={password} onChange={(event) => setPassword(event.target.value)} required />
            </label>
          )}
          {error && <p className="error">{error}</p>}
          <button className="primary-button" type="submit" disabled={isSubmitting}>
            {isSubmitting ? 'Working...' : mode === 'create' ? 'Create account' : mode === 'reset' ? 'Request reset' : 'Sign in'}
          </button>
        </form>
        <button className="text-button" type="button" onClick={() => setMode(mode === 'create' ? 'signin' : 'create')}>
          {mode === 'create' ? 'I already have an account' : 'Create an account'}
        </button>
        {mode !== 'reset' && (
          <button className="text-button" type="button" onClick={() => setMode('reset')}>
            Forgot password?
          </button>
        )}
        {mode === 'reset' && (
          <button className="text-button" type="button" onClick={() => setMode('signin')}>
            Back to sign in
          </button>
        )}
      </section>
    </main>
  );
}

function OnboardingScreen({
  session,
  onComplete,
  onLogout,
}: {
  session: AuthSession;
  onComplete: (payload: { role: string; primaryGame: string; registryUrl: string }) => Promise<void>;
  onLogout: () => Promise<void>;
}) {
  const [role, setRole] = useState('player');
  const [primaryGame, setPrimaryGame] = useState('Unity');
  const [registryUrl, setRegistryUrl] = useState('http://localhost:4567');
  const [isSubmitting, setIsSubmitting] = useState(false);

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    setIsSubmitting(true);
    try {
      await onComplete({ role, primaryGame, registryUrl });
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <main className="center-shell">
      <section className="auth-panel wide">
        <p className="eyebrow">First run setup</p>
        <h1>Set up your workspace</h1>
        <p className="panel-copy">
          Hi {session.user.displayName}. These choices personalize the marketplace and are saved to your account session.
        </p>
        <form className="auth-form" onSubmit={submit}>
          <label>
            I am using TheUnlocker as
            <select value={role} onChange={(event) => setRole(event.target.value)}>
              <option value="player">Player</option>
              <option value="mod-author">Mod author</option>
              <option value="publisher">Publisher</option>
              <option value="admin">Registry admin</option>
            </select>
          </label>
          <label>
            Primary game/runtime
            <select value={primaryGame} onChange={(event) => setPrimaryGame(event.target.value)}>
              <option>Unity</option>
              <option>Unreal</option>
              <option>Minecraft</option>
              <option>Multi-game</option>
            </select>
          </label>
          <label>
            Registry URL
            <input value={registryUrl} onChange={(event) => setRegistryUrl(event.target.value)} />
          </label>
          <button className="primary-button" type="submit" disabled={isSubmitting}>
            {isSubmitting ? 'Saving...' : 'Finish onboarding'}
          </button>
        </form>
        <button className="text-button" type="button" onClick={onLogout}>Sign out</button>
      </section>
    </main>
  );
}

createRoot(document.getElementById('root')!).render(
  <BrowserRouter>
    <Routes>
      <Route path="/" element={<App />} />
      <Route path="/mods/:modId" element={<App routePage="mods" />} />
      <Route path="/publishers/:publisherId" element={<App routePage="publisher" />} />
      <Route path="/publisher-analytics" element={<App routePage="analytics" />} />
      <Route path="/modpacks/:modpackId" element={<App routePage="modpack" />} />
      <Route path="/cloud-modpacks" element={<App routePage="cloudpacks" />} />
      <Route path="/collections" element={<App routePage="collections" />} />
      <Route path="/compatibility" element={<App routePage="compatibility" />} />
      <Route path="/assistant" element={<App routePage="assistant" />} />
      <Route path="/package-diff" element={<App routePage="diff" />} />
      <Route path="/lab" element={<App routePage="lab" />} />
      <Route path="/builds" element={<App routePage="builds" />} />
      <Route path="/federation" element={<App routePage="federation" />} />
      <Route path="/trust" element={<App routePage="trust" />} />
      <Route path="/policy-lab" element={<App routePage="policylab" />} />
      <Route path="/operations" element={<App routePage="operations" />} />
      <Route path="/control-center" element={<App routePage="control" />} />
      <Route path="/devices" element={<App routePage="devices" />} />
      <Route path="/releases" element={<App routePage="releases" />} />
      <Route path="/governance" element={<App routePage="governance" />} />
      <Route path="/platform" element={<App routePage="platform" />} />
      <Route path="/docs-hub" element={<App routePage="docs" />} />
      <Route path="/settings" element={<App routePage="settings" />} />
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  </BrowserRouter>,
);

function MarketplaceList({
  mods,
  query,
  game,
  trust,
  setQuery,
  setGame,
  setTrust,
  openModDetails,
}: {
  mods: RegistryMod[];
  query: string;
  game: string;
  trust: string;
  setQuery: (value: string) => void;
  setGame: (value: string) => void;
  setTrust: (value: string) => void;
  openModDetails: (id: string) => void;
}) {
  return (
    <>
      <section className="toolbar" aria-label="Marketplace filters">
        <label>
          <Search size={16} />
          <input value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Search mods" />
        </label>
        <label>
          <Filter size={16} />
          <input value={game} onChange={(event) => setGame(event.target.value)} placeholder="Game or adapter" />
        </label>
        <select value={trust} onChange={(event) => setTrust(event.target.value)} aria-label="Trust level">
          <option value="">Any trust</option>
          <option value="Official">Official</option>
          <option value="Trusted Publisher">Trusted Publisher</option>
          <option value="Unknown">Unknown</option>
        </select>
      </section>

      <section className="grid" aria-label="Mods">
        {mods.map((mod) => (
          <article className="mod-card" key={mod.id}>
            <div className="card-topline">
              <span>{mod.gameId || 'multi-game'}</span>
              <span>{mod.versions[0]?.version ?? '0.0.0'}</span>
            </div>
            <h2>{mod.name}</h2>
            <p>{mod.description}</p>
            <div className="badges">
              <span><ShieldCheck size={14} /> {mod.trustLevel}</span>
              <span>{mod.status}</span>
              <span>{mod.rating?.toFixed(1)} rating</span>
            </div>
            <div className="tags">
              {mod.tags.map((tag) => <span key={tag}>{tag}</span>)}
            </div>
            <div className="card-actions">
              <button className="secondary-button" type="button" onClick={() => openModDetails(mod.id)}>Details</button>
              <a className="install" href={`theunlocker://install/${encodeURIComponent(mod.id)}`}>
                <Download size={16} />
                Install
              </a>
            </div>
          </article>
        ))}
      </section>
    </>
  );
}

function ModDetail({ mod, onBack }: { mod: RegistryMod; onBack: () => void }) {
  return (
    <section className="detail-page">
      <button className="text-button" type="button" onClick={onBack}>Back to marketplace</button>
      <div className="detail-layout">
        <article className="detail-main">
          <p className="eyebrow">{mod.gameId} mod</p>
          <h2>{mod.name}</h2>
          <p>{mod.description}</p>
          <div className="screenshot-strip">
            {mod.screenshots?.map((screenshot) => <img key={screenshot} src={screenshot} alt={`${mod.name} screenshot`} />)}
          </div>
          <h3>Versions</h3>
          {mod.versions.map((version) => (
            <div className="version-row" key={version.version}>
              <strong>{version.version}</strong>
              <span>{version.changelog}</span>
              <a href={version.downloadUrl}>Download</a>
            </div>
          ))}
          <h3>Reviews</h3>
          {mod.reviews?.map((review) => (
            <blockquote key={review.id}>
              <strong>{review.author} rated {review.rating}/5</strong>
              <p>{review.body}</p>
              {review.publisherReply && <p>Publisher reply: {review.publisherReply}</p>}
            </blockquote>
          ))}
        </article>
        <aside className="detail-side">
          <a className="install" href={`theunlocker://install/${encodeURIComponent(mod.id)}`}><Download size={16} />Install with TheUnlocker</a>
          <h3>Permissions</h3>
          <ul>{mod.permissions.map((permission) => <li key={permission}>{permission}</li>)}</ul>
          <h3>Dependencies</h3>
          <ul>{mod.dependencies?.map((dependency) => <li key={dependency}>{dependency}</li>)}</ul>
          <h3>Publisher</h3>
          <p>{mod.publisher?.name} {mod.publisher?.verified ? '(verified)' : ''}</p>
          <code>{mod.publisher?.publicKey}</code>
        </aside>
      </div>
    </section>
  );
}

function AccountSettings({
  session,
  security,
  onVerifyEmail,
  onSave,
}: {
  session: AuthSession;
  security: AccountSecurityState | null;
  onVerifyEmail: () => Promise<string>;
  onSave: (payload: { displayName: string; primaryGame: string; registryUrl: string; password?: string }) => Promise<void>;
}) {
  const [displayName, setDisplayName] = useState(session.user.displayName);
  const [primaryGame, setPrimaryGame] = useState(session.user.primaryGame || 'Unity');
  const [registryUrl, setRegistryUrl] = useState(session.user.registryUrl || 'http://localhost:4567');
  const [password, setPassword] = useState('');
  const [status, setStatus] = useState('');

  return (
    <section className="detail-page narrow">
      <h2>Account settings</h2>
      <form className="auth-form" onSubmit={async (event) => {
        event.preventDefault();
        await onSave({ displayName, primaryGame, registryUrl, password: password || undefined });
        setPassword('');
        setStatus('Settings saved');
      }}>
        <label>Display name<input value={displayName} onChange={(event) => setDisplayName(event.target.value)} /></label>
        <label>Email<input value={`${session.user.email}${security?.emailVerified ? ' (verified)' : ''}`} disabled /></label>
        <label>Primary game<input value={primaryGame} onChange={(event) => setPrimaryGame(event.target.value)} /></label>
        <label>Registry URL<input value={registryUrl} onChange={(event) => setRegistryUrl(event.target.value)} /></label>
        <label>New password<input type="password" minLength={8} value={password} onChange={(event) => setPassword(event.target.value)} /></label>
        <button className="primary-button" type="submit">Save settings</button>
        {status && <p>{status}</p>}
      </form>
      <button className="secondary-button" type="button" onClick={async () => setStatus(await onVerifyEmail())}>Verify email</button>
      <h3>Trusted devices</h3>
      <ul>{(security?.trustedDevices ?? session.user.trustedDevices ?? ['Current browser session']).map((device) => <li key={device}>{device}</li>)}</ul>
      <h3>Sessions</h3>
      <div className="stack-list">
        {(security?.sessions ?? []).map((item) => (
          <div className="version-row" key={item.id}>
            <strong>{item.id}</strong>
            <span>{item.revoked ? 'revoked' : 'active'}</span>
            <span>{new Date(item.expiresAt).toLocaleString()}</span>
          </div>
        ))}
      </div>
      <h3>Login audit</h3>
      <div className="stack-list">
        {(security?.loginAudit ?? []).map((item) => (
          <div className="version-row" key={`${item.action}-${item.createdAt}`}>
            <strong>{item.action}</strong>
            <span>{item.success ? 'success' : 'failed'}</span>
            <span>{new Date(item.createdAt).toLocaleString()}</span>
          </div>
        ))}
      </div>
    </section>
  );
}

function PublisherPage({ mods, dashboard }: { mods: RegistryMod[]; dashboard: PublisherDashboard | null }) {
  const publishers = mods.map((mod) => mod.publisher).filter(Boolean);
  return (
    <section className="detail-page">
      <p className="eyebrow">Publisher portal</p>
      <h2>Uploads, signing keys, analytics, and moderation</h2>
      {dashboard && (
        <section className="metric-row dashboard-metrics">
          <span><strong>{dashboard.mods}</strong>mods</span>
          <span><strong>{dashboard.pendingUploads}</strong>pending uploads</span>
          <span><strong>{dashboard.openCrashReports}</strong>open crash reports</span>
          <span><strong>{dashboard.monthlyInstalls.toLocaleString()}</strong>monthly installs</span>
          <span><strong>{dashboard.conversionRate}</strong>conversion</span>
          <span><strong>{dashboard.averageRating.toFixed(1)}</strong>rating</span>
        </section>
      )}
      <section className="grid compact">
        {publishers.map((publisher) => publisher && (
          <article className="mod-card" key={publisher.id}>
            <h2>{publisher.name}</h2>
            <p>{publisher.verified ? 'Verified publisher' : 'Unverified publisher'}</p>
            <p>{publisher.stats.mods} mods, {publisher.stats.installs.toLocaleString()} installs, {publisher.stats.crashRate} crash rate</p>
            <code>{publisher.publicKey}</code>
            <ul>{publisher.trustHistory.map((item) => <li key={item}>{item}</li>)}</ul>
          </article>
        ))}
        {dashboard && (
          <article className="mod-card">
            <h2>Moderation and analytics</h2>
            <ul className="clean-list">{dashboard.moderationStates.map((item) => <li key={item}>{item}</li>)}</ul>
            <h3>Segments</h3>
            <div className="tags">{dashboard.analyticsSegments.map((item) => <span key={item}>{item}</span>)}</div>
          </article>
        )}
      </section>
    </section>
  );
}

function PublisherAnalyticsPage({ analytics }: { analytics: PublisherAnalyticsState | null }) {
  if (!analytics) {
    return (
      <section className="detail-page">
        <p className="eyebrow">Publisher analytics</p>
        <h2>Loading install trends and conversion data...</h2>
      </section>
    );
  }

  const maxInstalls = Math.max(...analytics.trend.map((point) => point.installs), 1);

  return (
    <section className="detail-page">
      <p className="eyebrow">Publisher analytics</p>
      <h2>Installs, conversion, crash health, ratings, adoption, and moderation outcomes</h2>
      <section className="metric-row dashboard-metrics">
        <span><strong>{analytics.installs.toLocaleString()}</strong>installs</span>
        <span><strong>{analytics.updates.toLocaleString()}</strong>updates</span>
        <span><strong>{analytics.marketplaceViews.toLocaleString()}</strong>views</span>
        <span><strong>{analytics.conversionRate}</strong>conversion</span>
        <span><strong>{analytics.averageRating.toFixed(1)}</strong>rating</span>
        <span><strong>{analytics.crashRate}</strong>crash rate</span>
      </section>
      <section className="analytics-chart" aria-label="Install trend">
        {analytics.trend.map((point) => (
          <div className="analytics-bar" key={point.date}>
            <span style={{ height: `${Math.max((point.installs / maxInstalls) * 100, 8)}%` }} />
            <strong>{point.installs.toLocaleString()}</strong>
            <small>{point.date.slice(5)}</small>
          </div>
        ))}
      </section>
      <section className="grid compact">
        <article className="mod-card analytics-card">
          <div className="section-title">
            <Activity size={20} />
            <h2>Conversion funnel</h2>
          </div>
          <div className="stack-list">
            {analytics.funnel.map((stage) => (
              <div className="version-row" key={stage.stage}>
                <strong>{stage.stage}</strong>
                <span>{stage.count.toLocaleString()}</span>
                <span>{stage.rate}</span>
              </div>
            ))}
          </div>
        </article>
        <article className="mod-card analytics-card">
          <h2>Version adoption</h2>
          <div className="stack-list">
            {analytics.adoption.map((item) => (
              <div className="version-row" key={`${item.version}-${item.ring}`}>
                <strong>{item.version}</strong>
                <span>{item.ring}</span>
                <span>{item.users.toLocaleString()} users</span>
                <span>{item.percentage}</span>
              </div>
            ))}
          </div>
        </article>
      </section>
      <section className="release-list">
        {analytics.topMods.map((mod) => (
          <article className="release-card analytics-mod" key={mod.modId}>
            <div className="card-topline">
              <span>{mod.modId}</span>
              <span>{mod.conversionRate} conversion</span>
            </div>
            <h3>{mod.name}</h3>
            <div className="metric-row">
              <span><strong>{mod.installs.toLocaleString()}</strong>installs</span>
              <span><strong>{mod.updates.toLocaleString()}</strong>updates</span>
              <span><strong>{mod.rating.toFixed(1)}</strong>rating</span>
              <span><strong>{mod.crashRate}</strong>crash rate</span>
            </div>
          </article>
        ))}
      </section>
      <section className="mod-card analytics-card">
        <h2>Moderation outcomes</h2>
        <div className="stack-list">
          {analytics.moderationOutcomes.map((item) => (
            <div className="version-row" key={item.status}>
              <strong>{item.status}</strong>
              <span>{item.count} releases</span>
              <span>{item.averageHours}h average</span>
            </div>
          ))}
        </div>
        <p>Generated for {analytics.displayName} at {new Date(analytics.generatedAt).toLocaleString()}.</p>
      </section>
    </section>
  );
}

function ModpackPage({ mods, graph }: { mods: RegistryMod[]; graph: DependencyGraph | null }) {
  return (
    <section className="detail-page">
      <h2>Curated modpack</h2>
      <p>This lockfile-backed pack installs exact approved versions and can be shared through a desktop protocol link.</p>
      <div className="version-row"><strong>Vanilla+</strong><span>{mods.length} compatible mods</span><a href="theunlocker://install-pack/vanilla-plus">Install pack</a></div>
      <pre>{JSON.stringify({ name: 'Vanilla+', mods: mods.map((mod) => ({ id: mod.id, version: mod.versions[0]?.version })) }, null, 2)}</pre>
      {graph && <DependencyGraphView graph={graph} />}
    </section>
  );
}

function CloudModpackPage({ cloudModpacks }: { cloudModpacks: CloudModpackState | null }) {
  if (!cloudModpacks) {
    return (
      <section className="detail-page">
        <p className="eyebrow">Cloud modpack sharing</p>
        <h2>Loading immutable lockfiles and install links...</h2>
      </section>
    );
  }

  return (
    <section className="detail-page">
      <p className="eyebrow">Cloud modpack sharing</p>
      <h2>Immutable lockfiles, one-click installs, compatibility status, and rollback metadata</h2>
      <section className="metric-row release-summary">
        <span><strong>{cloudModpacks.featuredCount}</strong>featured packs</span>
        <span><strong>{cloudModpacks.sharedInstalls.toLocaleString()}</strong>shared installs</span>
        <span><strong>{cloudModpacks.immutableLockfiles}</strong>lockfiles</span>
        <span><strong>{new Date(cloudModpacks.lastIndexedAt).toLocaleTimeString()}</strong>last indexed</span>
      </section>
      <section className="grid compact">
        {cloudModpacks.modpacks.map((pack) => (
          <article className={`mod-card cloud-modpack ${pack.trustDecision}`} key={pack.id}>
            <div className="section-title">
              <UploadCloud size={20} />
              <h2>{pack.name}</h2>
            </div>
            <div className="card-topline">
              <span>{pack.version}</span>
              <span>{pack.updateRing}</span>
            </div>
            <p>{pack.description}</p>
            <div className="metric-row">
              <span><strong>{pack.modCount}</strong>mods</span>
              <span><strong>{pack.downloadSizeMb} MB</strong>download</span>
              <span><strong>{pack.compatibility}</strong>compatibility</span>
              <span><strong>{pack.rollbackVersion}</strong>rollback</span>
            </div>
            <div className="tags">
              {pack.maintainers.map((maintainer) => <span key={maintainer}>{maintainer}</span>)}
              {pack.badges.map((badge) => <span key={badge}>{badge}</span>)}
            </div>
            <code>{pack.lockfileSha256}</code>
            <a className="install" href={pack.installUrl}><Download size={16} />Install pack</a>
            <a className="secondary-link" href={pack.lockfileUrl}>{pack.lockfileUrl}</a>
            <small>Compatibility checked {new Date(pack.lastCompatibilityAt).toLocaleString()}</small>
          </article>
        ))}
      </section>
    </section>
  );
}

function CollectionsPage({ collections }: { collections: MarketplaceCollection[] }) {
  return (
    <section className="detail-page">
      <p className="eyebrow">Marketplace collections</p>
      <h2>Curated packs, editor picks, and starter sets</h2>
      <section className="grid compact">
        {collections.map((collection) => (
          <article className="mod-card" key={collection.id}>
            <div className="card-topline">
              <span>{collection.curator}</span>
              <span>{collection.modIds.length} mods</span>
            </div>
            <h2>{collection.name}</h2>
            <p>{collection.description}</p>
            <div className="tags">{collection.badges.map((badge) => <span key={badge}>{badge}</span>)}</div>
            <pre>{JSON.stringify({ id: collection.id, mods: collection.modIds }, null, 2)}</pre>
          </article>
        ))}
      </section>
    </section>
  );
}

function CompatibilityPage({ signals, graph }: { signals: CompatibilitySignal[]; graph: DependencyGraph | null }) {
  return (
    <section className="detail-page">
      <p className="eyebrow">Compatibility intelligence</p>
      <h2>Signals from installs, crashes, and known bridges</h2>
      <section className="grid compact">
        {signals.map((signal) => (
          <article className="mod-card" key={`${signal.modA}-${signal.modB}`}>
            <div className="card-topline">
              <span>{signal.risk} risk</span>
              <span>{signal.installCount.toLocaleString()} installs</span>
            </div>
            <h2>{signal.modA} + {signal.modB}</h2>
            <p>{signal.recommendation}</p>
            <div className="metric-row">
              <span><strong>{signal.crashCount}</strong>crashes</span>
              <span><strong>{Math.round((signal.crashCount / Math.max(signal.installCount, 1)) * 1000) / 10}%</strong>reported rate</span>
            </div>
          </article>
        ))}
      </section>
      {graph && <DependencyGraphView graph={graph} />}
    </section>
  );
}

function AICompatibilityAssistantPage({ assistant }: { assistant: AICompatibilityAssistantState | null }) {
  if (!assistant) {
    return (
      <section className="detail-page">
        <p className="eyebrow">AI compatibility assistant</p>
        <h2>Loading compatibility suggestions...</h2>
      </section>
    );
  }

  return (
    <section className="detail-page">
      <p className="eyebrow">AI compatibility assistant</p>
      <h2>Load order fixes, bridge patch hints, permission warnings, and migration notes</h2>
      <section className="assistant-summary">
        <div className="section-title">
          <Sparkles size={22} />
          <h2>{assistant.subject}</h2>
        </div>
        <p>{assistant.summary}</p>
        <blockquote>{assistant.recommendedAction}</blockquote>
        <div className="metric-row">
          <span><strong>{assistant.confidence}</strong>confidence</span>
          <span><strong>{assistant.overallRisk}</strong>overall risk</span>
          <span><strong>{new Date(assistant.generatedAt).toLocaleTimeString()}</strong>generated</span>
          <span><strong>{assistant.analysisId}</strong>analysis</span>
        </div>
      </section>
      <section className="grid compact">
        {assistant.suggestions.map((suggestion) => (
          <article className={`mod-card assistant-card ${suggestion.severity}`} key={suggestion.id}>
            <div className="card-topline">
              <span>{suggestion.kind}</span>
              <span>{suggestion.severity}</span>
            </div>
            <h2>{suggestion.title}</h2>
            <p>{suggestion.detail}</p>
            <h3>Affected items</h3>
            <div className="tags">{suggestion.affectedIds.map((id) => <span key={id}>{id}</span>)}</div>
            <h3>Suggested actions</h3>
            <ul className="clean-list">
              {suggestion.actions.map((action) => <li key={action}>{action}</li>)}
            </ul>
          </article>
        ))}
      </section>
      <section className="release-list">
        {assistant.evidence.map((item) => (
          <article className="release-card assistant-evidence" key={`${item.source}-${item.signal}`}>
            <div className="card-topline">
              <span>{item.source}</span>
              <span>evidence</span>
            </div>
            <p>{item.signal}</p>
          </article>
        ))}
      </section>
    </section>
  );
}

function PackageDiffPage({ diff }: { diff: PackageDiffState | null }) {
  if (!diff) {
    return (
      <section className="detail-page">
        <p className="eyebrow">Package diff</p>
        <h2>Loading version comparison...</h2>
      </section>
    );
  }

  return (
    <section className="detail-page">
      <p className="eyebrow">Package diff center</p>
      <h2>Compare permissions, dependencies, files, migrations, changelog, and rollback before update</h2>
      <section className="assistant-summary package-diff-summary">
        <div className="section-title">
          <FileDiff size={22} />
          <h2>{diff.name} {diff.fromVersion} to {diff.toVersion}</h2>
        </div>
        <p>{diff.summary}</p>
        <div className="metric-row">
          <span><strong>{diff.publisher}</strong>publisher</span>
          <span><strong>{diff.riskChange}</strong>risk change</span>
          <span><strong>{diff.decision}</strong>decision</span>
          <span><strong>{new Date(diff.generatedAt).toLocaleTimeString()}</strong>generated</span>
        </div>
      </section>
      <section className="grid compact">
        <article className="mod-card diff-card approval">
          <h2>Permission changes</h2>
          <div className="stack-list">
            {diff.permissionChanges.map((item) => (
              <div className={`version-row diff-row ${item.change}`} key={`${item.permission}-${item.change}`}>
                <strong>{item.permission}</strong>
                <span>{item.change}</span>
                <span>{item.reason}</span>
              </div>
            ))}
          </div>
        </article>
        <article className="mod-card diff-card dependency">
          <h2>Dependency changes</h2>
          <div className="stack-list">
            {diff.dependencyChanges.map((item) => (
              <div className={`version-row diff-row ${item.change}`} key={`${item.dependency}-${item.change}`}>
                <strong>{item.dependency}</strong>
                <span>{item.from || 'none'}</span>
                <span>{item.to}</span>
                <span>{item.change}</span>
              </div>
            ))}
          </div>
        </article>
        <article className="mod-card diff-card migration">
          <h2>Settings migrations</h2>
          <div className="stack-list">
            {diff.settingsMigrations.map((migration) => (
              <div className="version-row" key={migration.id}>
                <strong>{migration.id}</strong>
                <span>{migration.from} to {migration.to}</span>
                <span>{migration.status}</span>
              </div>
            ))}
          </div>
          {diff.settingsMigrations.map((migration) => <p key={`${migration.id}-description`}>{migration.description}</p>)}
        </article>
        <article className="mod-card diff-card rollback">
          <h2>Rollback point</h2>
          <p>{diff.rollback.strategy}</p>
          <div className="metric-row">
            <span><strong>{diff.rollback.availableVersion}</strong>available</span>
            <span><strong>{diff.rollback.snapshotId}</strong>snapshot</span>
          </div>
        </article>
      </section>
      <section className="release-list">
        {diff.fileChanges.map((file) => (
          <article className={`release-card diff-file ${file.change}`} key={file.path}>
            <div className="card-topline">
              <span>{file.change}</span>
              <span>{file.sizeDeltaKb >= 0 ? '+' : ''}{file.sizeDeltaKb} KB</span>
            </div>
            <h3>{file.path}</h3>
            <code>{file.sha256}</code>
          </article>
        ))}
      </section>
      <section className="mod-card">
        <h2>Changelog</h2>
        <ul className="clean-list">
          {diff.changelog.map((entry) => <li key={entry}>{entry}</li>)}
        </ul>
      </section>
    </section>
  );
}

function CompatibilityLabPage({ lab }: { lab: CompatibilityLabState | null }) {
  if (!lab) {
    return (
      <section className="detail-page">
        <p className="eyebrow">Compatibility lab</p>
        <h2>Loading sandbox job results...</h2>
      </section>
    );
  }

  return (
    <section className="detail-page">
      <p className="eyebrow">Compatibility lab</p>
      <h2>Adapter sandbox tests, queue health, and release recommendations</h2>
      <section className="metric-row release-summary">
        <span><strong>{lab.activeJobs}</strong>active jobs</span>
        <span><strong>{lab.queuedJobs}</strong>queued</span>
        <span><strong>{lab.failedJobs}</strong>failed</span>
        <span><strong>{lab.passRate}</strong>pass rate</span>
        <span><strong>{lab.adaptersCovered}</strong>adapters</span>
      </section>
      <section className="grid compact">
        <article className="mod-card">
          <div className="section-title">
            <FlaskConical size={20} />
            <h2>Worker queue</h2>
          </div>
          <div className="metric-row">
            <span><strong>{lab.queue.running}</strong>running</span>
            <span><strong>{lab.queue.queued}</strong>queued</span>
            <span><strong>{lab.queue.failed}</strong>failed</span>
            <span><strong>{lab.queue.deadLetter}</strong>dead letter</span>
            <span><strong>{lab.queue.averageSeconds}s</strong>average</span>
          </div>
          <p>Last completed run {new Date(lab.lastRunAt).toLocaleString()}.</p>
        </article>
        {lab.adapters.map((adapter) => (
          <article className="mod-card lab-adapter-card" key={adapter.id}>
            <div className="card-topline">
              <span>{adapter.id}</span>
              <span>{adapter.status}</span>
            </div>
            <h2>{adapter.name}</h2>
            <div className="tags">{adapter.supportedVersions.map((version) => <span key={version}>{version}</span>)}</div>
            <small>Last test {new Date(adapter.lastTestAt).toLocaleString()}</small>
          </article>
        ))}
      </section>
      <section className="release-list">
        {lab.jobs.map((job) => (
          <article className={`release-card lab-result ${job.result}`} key={job.id}>
            <div className="card-topline">
              <span>{job.adapter}</span>
              <span>{job.status}</span>
            </div>
            <h3>{job.modpackId}</h3>
            <p>{job.recommendation}</p>
            <div className="metric-row">
              <span><strong>{job.result}</strong>result</span>
              <span><strong>{job.durationSeconds}s</strong>duration</span>
              <span><strong>{job.gameId}</strong>game</span>
              <span><strong>{new Date(job.startedAt).toLocaleTimeString()}</strong>started</span>
            </div>
            {job.crashSignature && <code>{job.crashSignature}</code>}
            <ul className="clean-list">
              {job.findings.map((finding) => <li key={finding}>{finding}</li>)}
            </ul>
          </article>
        ))}
      </section>
    </section>
  );
}

function BuildFarmPage({ buildFarm }: { buildFarm: BuildFarmState | null }) {
  if (!buildFarm) {
    return (
      <section className="detail-page">
        <p className="eyebrow">Build farm</p>
        <h2>Loading reproducible build jobs...</h2>
      </section>
    );
  }

  return (
    <section className="detail-page">
      <p className="eyebrow">Hosted build farm</p>
      <h2>Reproducible builds, signatures, SBOMs, scans, and provenance</h2>
      <section className="metric-row release-summary">
        <span><strong>{buildFarm.activeJobs}</strong>active jobs</span>
        <span><strong>{buildFarm.queuedJobs}</strong>queued</span>
        <span><strong>{buildFarm.successfulToday}</strong>successful today</span>
        <span><strong>{buildFarm.failedToday}</strong>failed today</span>
        <span><strong>{buildFarm.averageBuildTime}</strong>average build</span>
      </section>
      <section className="grid compact">
        {buildFarm.workers.map((worker) => (
          <article className="mod-card build-worker-card" key={worker.id}>
            <div className="section-title">
              <Cpu size={20} />
              <h2>{worker.id}</h2>
            </div>
            <div className="card-topline">
              <span>{worker.pool}</span>
              <span>{worker.status}</span>
            </div>
            <p>Current job: {worker.currentJob || 'idle'}</p>
            <div className="tags">{worker.capabilities.map((capability) => <span key={capability}>{capability}</span>)}</div>
            <small>Heartbeat {new Date(worker.lastHeartbeatAt).toLocaleTimeString()}</small>
          </article>
        ))}
      </section>
      <section className="release-list">
        {buildFarm.jobs.map((job) => (
          <article className={`release-card build-job ${job.status}`} key={job.id}>
            <div className="card-topline">
              <span>{job.stage}</span>
              <span>{job.promotionRing}</span>
            </div>
            <h3>{job.packageId} {job.version}</h3>
            <p>{job.releaseRecommendation}</p>
            <div className="metric-row">
              <span><strong>{job.status}</strong>status</span>
              <span><strong>{job.durationSeconds}s</strong>duration</span>
              <span><strong>{job.publisher}</strong>publisher</span>
              <span><strong>{job.sourceCommit}</strong>commit</span>
            </div>
            <div className="tags">
              <span>{job.reproducible ? 'reproducible' : 'not reproducible'}</span>
              <span>{job.sbomGenerated ? 'SBOM generated' : 'SBOM missing'}</span>
              <span>{job.signatureVerified ? 'signature verified' : 'signature missing'}</span>
              <span>{job.provenanceAttested ? 'attested' : 'no attestation'}</span>
              <span>{job.malwareScan}</span>
            </div>
            <code>{job.artifactSha256}</code>
            {job.ciRunUrl && <a className="secondary-link" href={job.ciRunUrl}>{job.ciRunUrl}</a>}
            <small>Started {new Date(job.startedAt).toLocaleString()}</small>
          </article>
        ))}
      </section>
    </section>
  );
}

function FederationPage({ federation }: { federation: RegistryFederationState | null }) {
  if (!federation) {
    return (
      <section className="detail-page">
        <p className="eyebrow">Registry federation</p>
        <h2>Loading federated registry results...</h2>
      </section>
    );
  }

  return (
    <section className="detail-page">
      <p className="eyebrow">Registry federation</p>
      <h2>Official, private, local, and community registries with policy-aware results</h2>
      <section className="metric-row release-summary">
        <span><strong>{federation.connected}</strong>connected</span>
        <span><strong>{federation.healthy}</strong>healthy</span>
        <span><strong>{federation.blockedResults}</strong>blocked results</span>
        <span><strong>{federation.defaultTrustLevel}</strong>required trust</span>
        <span><strong>{federation.policyVersion}</strong>policy</span>
      </section>
      <section className="grid compact">
        {federation.registries.map((registry) => (
          <article className={`mod-card federation-registry ${registry.status}`} key={registry.id}>
            <div className="section-title">
              <GitBranch size={20} />
              <h2>{registry.name}</h2>
            </div>
            <div className="card-topline">
              <span>{registry.kind}</span>
              <span>{registry.status}</span>
            </div>
            <p>{registry.trustPolicy}</p>
            <div className="metric-row">
              <span><strong>{registry.packagesIndexed.toLocaleString()}</strong>packages</span>
              <span><strong>{registry.latencyMs}ms</strong>latency</span>
              <span><strong>{registry.priority}</strong>priority</span>
              <span><strong>{registry.allowUnsigned ? 'allowed' : 'blocked'}</strong>unsigned</span>
            </div>
            <a className="secondary-link" href={registry.url}>{registry.url}</a>
            <small>Synced {new Date(registry.lastSyncAt).toLocaleString()}</small>
          </article>
        ))}
      </section>
      <section className="release-list">
        {federation.results.map((result) => (
          <article className={`release-card federation-result ${result.allowedByPolicy ? 'allowed' : 'blocked'}`} key={`${result.registryId}-${result.packageId}`}>
            <div className="card-topline">
              <span>{result.registryId}</span>
              <span>{result.allowedByPolicy ? 'allowed' : 'blocked'}</span>
            </div>
            <h3>{result.name} {result.version}</h3>
            <p>{result.policyDecision}</p>
            <div className="metric-row">
              <span><strong>{result.score}</strong>score</span>
              <span><strong>{result.publisher}</strong>publisher</span>
              <span><strong>{result.trustLevel}</strong>trust</span>
              <span><strong>{result.packageId}</strong>package</span>
            </div>
          </article>
        ))}
      </section>
    </section>
  );
}

function TrustReputationPage({ trust }: { trust: TrustReputationState | null }) {
  if (!trust) {
    return (
      <section className="detail-page">
        <p className="eyebrow">Trust and reputation</p>
        <h2>Loading package risk explanations...</h2>
      </section>
    );
  }

  return (
    <section className="detail-page">
      <p className="eyebrow">Trust and reputation</p>
      <h2>Explainable package scores, publisher reputation, and advisories</h2>
      <section className="metric-row release-summary">
        <span><strong>{trust.averageScore}</strong>average score</span>
        <span><strong>{trust.flaggedPackages}</strong>flagged packages</span>
        <span><strong>{trust.trustedPublishers}</strong>trusted publishers</span>
        <span><strong>{trust.requiredTrust}</strong>required trust</span>
        <span><strong>{trust.policyVersion}</strong>policy</span>
      </section>
      <section className="release-list">
        {trust.packages.map((item) => (
          <article className={`release-card trust-package ${item.decision}`} key={item.packageId}>
            <div className="card-topline">
              <span>{item.trustLevel}</span>
              <span>{item.decision}</span>
            </div>
            <h3>{item.name} {item.version}</h3>
            <p>{item.recommendation}</p>
            <div className="metric-row">
              <span><strong>{item.score}</strong>score</span>
              <span><strong>{item.publisher}</strong>publisher</span>
              <span><strong>{item.packageId}</strong>package</span>
            </div>
            <div className="trust-factor-grid">
              <div>
                <h4>Positive factors</h4>
                {item.positiveFactors.length === 0 ? <p>No positive factors recorded.</p> : (
                  <ul className="clean-list">{item.positiveFactors.map((factor) => <li key={factor}>{factor}</li>)}</ul>
                )}
              </div>
              <div>
                <h4>Risk factors</h4>
                {item.riskFactors.length === 0 ? <p>No risk factors recorded.</p> : (
                  <ul className="clean-list">{item.riskFactors.map((factor) => <li key={factor}>{factor}</li>)}</ul>
                )}
              </div>
            </div>
          </article>
        ))}
      </section>
      <section className="grid compact">
        <article className="mod-card">
          <h2>Publisher reputation</h2>
          <div className="stack-list">
            {trust.publishers.map((publisher) => (
              <div className="version-row" key={publisher.publisherId}>
                <strong>{publisher.displayName}</strong>
                <span>{publisher.trustLevel}</span>
                <span>{publisher.verified ? 'verified' : 'unverified'}</span>
                <span>{publisher.reputationScore} score</span>
                <span>{publisher.crashRate} crash rate</span>
              </div>
            ))}
          </div>
        </article>
        <article className="mod-card">
          <h2>Advisories</h2>
          <div className="stack-list">
            {trust.advisories.map((advisory) => (
              <div className={`notice ${advisory.severity === 'high' ? 'critical' : 'warning'}`} key={advisory.id}>
                <strong>{advisory.id} · {advisory.packageId}</strong>
                <p>{advisory.summary}</p>
                <small>{advisory.severity} · {advisory.status} · {new Date(advisory.publishedAt).toLocaleString()}</small>
              </div>
            ))}
          </div>
        </article>
      </section>
    </section>
  );
}

function PolicySimulationPage({ simulations }: { simulations: PolicySimulationState | null }) {
  if (!simulations) {
    return (
      <section className="detail-page">
        <p className="eyebrow">Policy Lab</p>
        <h2>Loading enterprise policy simulations...</h2>
      </section>
    );
  }

  return (
    <section className="detail-page">
      <p className="eyebrow">Policy Lab</p>
      <h2>Preview install decisions before rules reach users</h2>
      <section className="metric-row release-summary">
        <span><strong>{simulations.overallDecision}</strong>overall</span>
        <span><strong>{simulations.allowedCount}</strong>allowed</span>
        <span><strong>{simulations.reviewCount}</strong>review</span>
        <span><strong>{simulations.blockedCount}</strong>blocked</span>
        <span><strong>{simulations.policyVersion}</strong>policy</span>
      </section>
      <section className="grid compact">
        {simulations.rules.map((rule) => (
          <article className={`mod-card policy-rule-card ${rule.severity}`} key={rule.id}>
            <div className="card-topline">
              <span>{rule.enabled ? 'enabled' : 'disabled'}</span>
              <span>{rule.severity}</span>
            </div>
            <h2>{rule.label}</h2>
            <p>{rule.description}</p>
            <code>{rule.id}</code>
          </article>
        ))}
      </section>
      <section className="release-list">
        {simulations.scenarios.map((scenario) => (
          <article className={`release-card policy-scenario ${scenario.decision}`} key={scenario.id}>
            <div className="card-topline">
              <span>{scenario.registry}</span>
              <span>{scenario.decision}</span>
            </div>
            <h3>{scenario.title}</h3>
            <p>{scenario.packageId} {scenario.version} from {scenario.trustLevel} on the {scenario.updateRing} ring.</p>
            <div className="metric-row">
              <span><strong>{scenario.score}</strong>score</span>
              <span><strong>{scenario.requestedPermissions.length}</strong>permissions</span>
              <span><strong>{scenario.findings.length}</strong>findings</span>
            </div>
            <div className="tags">
              {scenario.requestedPermissions.map((permission) => <span key={permission}>{permission}</span>)}
            </div>
            <div className="stack-list">
              {scenario.findings.map((finding) => (
                <div className={`notice ${finding.severity === 'critical' ? 'critical' : finding.severity === 'warning' ? 'warning' : 'info'}`} key={`${scenario.id}-${finding.ruleId}-${finding.message}`}>
                  <strong>{finding.ruleId}</strong>
                  <p>{finding.message}</p>
                </div>
              ))}
            </div>
          </article>
        ))}
      </section>
      <article className="mod-card">
        <h2>Recommended rollout actions</h2>
        <ul className="clean-list">
          {simulations.recommendedActions.map((action) => <li key={action}>{action}</li>)}
        </ul>
      </article>
    </section>
  );
}

function ControlCenterPage({
  queue,
  recoveryPlan,
  workflowRules,
}: {
  queue: InstallQueueItem[];
  recoveryPlan: RecoveryStep[];
  workflowRules: WorkflowRule[];
}) {
  return (
    <section className="detail-page">
      <p className="eyebrow">Control center</p>
      <h2>Install queue, recovery plan, and automation rules</h2>
      <section className="grid compact">
        <article className="mod-card">
          <h2>Install queue</h2>
          <div className="stack-list">
            {queue.map((item) => (
              <div className="version-row" key={item.id}>
                <strong>{item.packageId} {item.version}</strong>
                <span>{item.currentStage}</span>
                <span>{item.progress}%</span>
                <span>{item.rollback}</span>
              </div>
            ))}
          </div>
        </article>
        <article className="mod-card">
          <h2>Crash recovery wizard</h2>
          <ul className="clean-list">
            {recoveryPlan.map((step) => <li key={step.id}><strong>{step.label}</strong>: {step.description}</li>)}
          </ul>
        </article>
        <article className="mod-card">
          <h2>Workflow automation</h2>
          <div className="stack-list">
            {workflowRules.map((rule) => (
              <div className="version-row" key={rule.id}>
                <strong>{rule.trigger}</strong>
                <span>{rule.condition}</span>
                <span>{rule.action}</span>
                <span>{rule.enabled ? 'enabled' : 'paused'}</span>
              </div>
            ))}
          </div>
        </article>
      </section>
    </section>
  );
}

function DeviceFleetPage({ fleet }: { fleet: DeviceFleetState | null }) {
  if (!fleet) {
    return (
      <section className="detail-page">
        <p className="eyebrow">Device fleet</p>
        <h2>Loading remote orchestration clients...</h2>
      </section>
    );
  }

  return (
    <section className="detail-page">
      <p className="eyebrow">Device fleet</p>
      <h2>Remote orchestration, trusted clients, and push install readiness</h2>
      <section className="metric-row release-summary">
        <span><strong>{fleet.onlineDevices}</strong>online devices</span>
        <span><strong>{fleet.pendingCommands}</strong>pending commands</span>
        <span><strong>{new Date(fleet.lastFleetSyncAt).toLocaleTimeString()}</strong>last sync</span>
        <span><strong>{fleet.orchestrationKey}</strong>fleet key</span>
      </section>
      <section className="grid compact">
        {fleet.devices.map((device) => (
          <article className="mod-card device-card" key={device.id}>
            <div className="section-title">
              <Cable size={20} />
              <h2>{device.name}</h2>
            </div>
            <div className="card-topline">
              <span>{device.kind}</span>
              <span>{device.status}</span>
            </div>
            <div className="metric-row">
              <span><strong>{device.installedMods}</strong>mods</span>
              <span><strong>{device.activeProfile}</strong>profile</span>
              <span><strong>{device.canReceiveCommands ? 'ready' : 'blocked'}</strong>commands</span>
            </div>
            <div className="tags">
              <span>{device.trustLevel}</span>
              <span>{device.registryUrl}</span>
              {device.lanAddress && <span>{device.lanAddress}</span>}
            </div>
            <h3>Pending commands</h3>
            {device.pendingCommands.length === 0 ? (
              <p>No pending commands.</p>
            ) : (
              <div className="stack-list">
                {device.pendingCommands.map((command) => (
                  <div className="version-row" key={command.id}>
                    <strong>{command.type}</strong>
                    <span>{command.status}</span>
                    <span>{command.target}</span>
                  </div>
                ))}
              </div>
            )}
            <small>Last seen {new Date(device.lastSeenAt).toLocaleString()}</small>
          </article>
        ))}
      </section>
    </section>
  );
}

function ReleaseCenterPage({ releases }: { releases: DesktopReleaseState | null }) {
  if (!releases) {
    return (
      <section className="detail-page">
        <p className="eyebrow">Release center</p>
        <h2>Checking desktop release channels...</h2>
      </section>
    );
  }

  return (
    <section className="detail-page">
      <p className="eyebrow">Release center</p>
      <h2>Signed desktop updates, rollout rings, and rollback health</h2>
      <section className="metric-row release-summary">
        <span><strong>{releases.currentVersion}</strong>current version</span>
        <span><strong>{releases.activeChannel}</strong>active channel</span>
        <span><strong>{releases.autoUpdate ? 'enabled' : 'paused'}</strong>auto update</span>
        <span><strong>{releases.policy.requireSignedUpdates ? 'required' : 'optional'}</strong>signatures</span>
      </section>
      <section className="grid compact">
        <article className="mod-card">
          <div className="section-title">
            <ShieldCheck size={20} />
            <h2>Update policy</h2>
          </div>
          <ul className="clean-list">
            <li>Signed updates: {releases.policy.requireSignedUpdates ? 'required' : 'not required'}</li>
            <li>Prerelease builds: {releases.policy.allowPrerelease ? 'allowed' : 'blocked by default'}</li>
            <li>Rollback on failure: {releases.policy.rollbackOnFailure ? 'enabled' : 'disabled'}</li>
            <li>Allowed channels: {releases.policy.allowedChannels.join(', ')}</li>
          </ul>
        </article>
        <article className="mod-card">
          <div className="section-title">
            <Rocket size={20} />
            <h2>Rollback plan</h2>
          </div>
          <p>{releases.rollback.plan}</p>
          <div className="metric-row">
            <span><strong>{releases.rollback.availableVersion}</strong>rollback version</span>
            <span><strong>{new Date(releases.rollback.lastHealthyAt).toLocaleDateString()}</strong>last healthy</span>
          </div>
        </article>
      </section>
      <section className="release-list">
        {releases.releases.map((release) => (
          <article className="release-card" key={`${release.channel}-${release.version}`}>
            <div className="card-topline">
              <span>{release.channel}</span>
              <span>{release.health}</span>
            </div>
            <h3>{release.version}</h3>
            <p>{release.changelog}</p>
            <div className="release-progress" aria-label={`${release.rolloutPercentage}% rollout`}>
              <span style={{ width: `${release.rolloutPercentage}%` }} />
            </div>
            <div className="metric-row">
              <span><strong>{release.rolloutPercentage}%</strong>rollout</span>
              <span><strong>{release.signed ? 'yes' : 'no'}</strong>signed</span>
              <span><strong>{new Date(release.publishedAt).toLocaleDateString()}</strong>published</span>
            </div>
            <code>{release.sha256}</code>
          </article>
        ))}
      </section>
    </section>
  );
}

function GovernancePage({
  moderationQueue,
  notifications,
  policy,
  health,
}: {
  moderationQueue: ModerationQueueItem[];
  notifications: PlatformNotification[];
  policy: EffectivePolicy | null;
  health: RegistryServiceHealth[];
}) {
  return (
    <section className="detail-page">
      <p className="eyebrow">Governance</p>
      <h2>Moderation, notifications, policy, and service health</h2>
      <section className="grid compact">
        <article className="mod-card">
          <h2>Moderation queue</h2>
          <div className="stack-list">
            {moderationQueue.map((item) => (
              <div className="version-row" key={item.id}>
                <strong>{item.packageId}</strong>
                <span>{item.status}</span>
                <span>risk {item.riskScore}</span>
                <span>{item.publisher}</span>
              </div>
            ))}
          </div>
        </article>
        <article className="mod-card">
          <h2>Notification center</h2>
          <div className="stack-list">
            {notifications.map((item) => (
              <div className={`notice ${item.severity}`} key={item.id}>
                <strong>{item.title}</strong>
                <p>{item.body}</p>
                <small>{new Date(item.createdAt).toLocaleString()}</small>
              </div>
            ))}
          </div>
        </article>
        <article className="mod-card">
          <h2>Effective policy</h2>
          {policy && (
            <>
              <div className="metric-row">
                <span><strong>{policy.requiredTrustLevel}</strong>required trust</span>
                <span><strong>{policy.allowUnsignedMods ? 'yes' : 'no'}</strong>unsigned mods</span>
                <span><strong>{policy.version}</strong>version</span>
              </div>
              <h3>Allowed registries</h3>
              <div className="tags">{policy.allowedRegistries.map((item) => <span key={item}>{item}</span>)}</div>
              <h3>Blocked permissions</h3>
              <div className="tags">{policy.blockedPermissions.map((item) => <span key={item}>{item}</span>)}</div>
            </>
          )}
        </article>
        <article className="mod-card">
          <h2>Registry health</h2>
          <div className="stack-list">
            {health.map((item) => (
              <div className="version-row" key={item.service}>
                <strong>{item.service}</strong>
                <span>{item.status}</span>
                <span>{item.latencyMs}ms</span>
                <span>{item.detail}</span>
              </div>
            ))}
          </div>
        </article>
      </section>
    </section>
  );
}

function OperationsPage({
  upgrades,
  pipeline,
  graph,
}: {
  upgrades: ProductUpgrade[];
  pipeline: InstallPipelineStep[];
  graph: DependencyGraph | null;
}) {
  const byCategory = upgrades.reduce<Record<string, ProductUpgrade[]>>((groups, upgrade) => {
    groups[upgrade.category] ??= [];
    groups[upgrade.category].push(upgrade);
    return groups;
  }, {});

  return (
    <section className="detail-page">
      <p className="eyebrow">Operations center</p>
      <h2>Install, trust, recovery, and creator workflows</h2>
      <p>These surfaces turn the new platform upgrades into usable workflows for players, publishers, modpack maintainers, and local developers.</p>

      <section className="timeline" aria-label="Real mod install pipeline">
        {pipeline.map((step, index) => (
          <article className={`timeline-step ${step.status}`} key={step.id}>
            <span>{index + 1}</span>
            <div>
              <strong>{step.name}</strong>
              <p>{step.description}</p>
              <small>{step.status}</small>
            </div>
          </article>
        ))}
      </section>

      {Object.entries(byCategory).map(([category, items]) => (
        <section className="capability-band" key={category}>
          <h3>{category}</h3>
          <div className="grid compact">
            {items.map((upgrade) => (
              <article className="mod-card" key={upgrade.id}>
                <div className="card-topline">
                  <span>{upgrade.status}</span>
                  <span>{upgrade.category}</span>
                </div>
                <h2>{upgrade.name}</h2>
                <p>{upgrade.description}</p>
                <ul className="clean-list">
                  {upgrade.actions.map((action) => <li key={action}>{action}</li>)}
                </ul>
                <div className="metric-row">
                  {upgrade.metrics.map((metric) => (
                    <span key={metric.label}><strong>{metric.value}</strong>{metric.label}</span>
                  ))}
                </div>
              </article>
            ))}
          </div>
        </section>
      ))}

      {graph && <DependencyGraphView graph={graph} />}
    </section>
  );
}

function DependencyGraphView({ graph }: { graph: DependencyGraph }) {
  return (
    <section className="graph-panel" aria-label="Live dependency graph">
      <div className="section-title">
        <GitBranch size={20} />
        <h3>Live dependency graph</h3>
      </div>
      <div className="graph-grid">
        {graph.nodes.map((node) => (
          <div className={`graph-node ${node.kind}`} key={node.id}>
            <strong>{node.label}</strong>
            <span>{node.kind}</span>
          </div>
        ))}
      </div>
      <div className="edge-list">
        {graph.edges.map((edge) => (
          <span key={`${edge.from}-${edge.to}-${edge.label}`}>{edge.from} {'->'} {edge.label} {'->'} {edge.to}</span>
        ))}
      </div>
    </section>
  );
}

function DocumentationHub({ links }: { links: DocumentationLink[] }) {
  return (
    <section className="detail-page">
      <p className="eyebrow">In-app documentation hub</p>
      <h2>SDK, manifest, packaging, signing, and troubleshooting</h2>
      <p>The marketplace now exposes the documentation map that should also be embedded in the desktop app for offline mod-author help.</p>
      <section className="grid">
        {links.map((link) => (
          <article className="mod-card" key={link.href}>
            <div className="section-title">
              <BookOpen size={20} />
              <h2>{link.title}</h2>
            </div>
            <p>{link.description}</p>
            <a className="secondary-link" href={link.href}>{link.href}</a>
          </article>
        ))}
      </section>
    </section>
  );
}

function PlatformPage({ upgrades, productUpgrades }: { upgrades: PlatformUpgrade[]; productUpgrades: ProductUpgrade[] }) {
  return (
    <section className="detail-page">
      <p className="eyebrow">Platform roadmap</p>
      <h2>Major ecosystem capabilities</h2>
      <p>These modules move TheUnlocker from a local mod manager into a federated modding platform with trust, build, sync, automation, and release infrastructure.</p>
      <div className="feature-strip">
        <span><ShieldCheck size={16} /> {productUpgrades.length} product workflows</span>
        <span><UploadCloud size={16} /> publisher portal ready</span>
        <span><Activity size={16} /> compatibility intelligence</span>
        <span><Wrench size={16} /> local developer mode</span>
      </div>
      <section className="grid" aria-label="Major platform upgrades">
        {upgrades.map((upgrade) => (
          <article className="mod-card" key={upgrade.id}>
            <div className="card-topline">
              <span>{upgrade.status}</span>
              <span>{upgrade.surfaces.join(', ')}</span>
            </div>
            <h2>{upgrade.name}</h2>
            <p>{upgrade.description}</p>
            <div className="tags">
              {upgrade.surfaces.map((surface) => <span key={surface}>{surface}</span>)}
            </div>
          </article>
        ))}
      </section>
    </section>
  );
}
