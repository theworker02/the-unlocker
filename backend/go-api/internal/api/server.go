package api

import (
	"bytes"
	"encoding/json"
	"errors"
	"io"
	"net/http"
	"net/url"
	"strings"
	"time"
)

type Options struct {
	RegistryBaseURL string
	ModStore        ModStore
	HTTPClient      *http.Client
	StartedAt       time.Time
}

type Server struct {
	registryBaseURL string
	modStore        ModStore
	httpClient      *http.Client
	startedAt       time.Time
}

type healthResponse struct {
	Status       string `json:"status"`
	Service      string `json:"service"`
	RegistryBase string `json:"registryBase"`
	StartedAt    string `json:"startedAt"`
	CheckedAt    string `json:"checkedAt"`
}

type modVersion struct {
	Version     string `json:"version"`
	DownloadURL string `json:"downloadUrl"`
	SHA256      string `json:"sha256"`
	Changelog   string `json:"changelog"`
	CreatedAt   string `json:"createdAt"`
}

type registryMod struct {
	ID          string       `json:"id"`
	Name        string       `json:"name"`
	Author      string       `json:"author"`
	Description string       `json:"description"`
	Status      string       `json:"status"`
	GameID      string       `json:"gameId"`
	TrustLevel  string       `json:"trustLevel"`
	Tags        []string     `json:"tags"`
	Permissions []string     `json:"permissions"`
	Versions    []modVersion `json:"versions"`
}

func NewServer(options Options) *Server {
	client := options.HTTPClient
	if client == nil {
		client = &http.Client{Timeout: 10 * time.Second}
	}

	startedAt := options.StartedAt
	if startedAt.IsZero() {
		startedAt = time.Now().UTC()
	}

	return &Server{
		registryBaseURL: strings.TrimRight(options.RegistryBaseURL, "/"),
		modStore:        options.ModStore,
		httpClient:      client,
		startedAt:       startedAt,
	}
}

func (s *Server) Router() http.Handler {
	mux := http.NewServeMux()
	mux.HandleFunc("/", s.handleRoot)
	mux.HandleFunc("/health", s.handleHealth)
	mux.HandleFunc("/openapi.json", s.handleOpenAPI)
	mux.HandleFunc("/docs", s.handleDocs)
	mux.HandleFunc("/api/v1/health", s.handleHealth)
	mux.HandleFunc("/api/v1/auth/", s.handleAuthProxy)
	mux.HandleFunc("/api/v1/onboarding", s.requireAuth(s.handleOnboarding))
	mux.HandleFunc("/api/v1/account/settings", s.requireAuth(s.handleAccountSettings))
	mux.HandleFunc("/api/v1/account/security", s.requireAuth(s.handleAccountSecurity))
	mux.HandleFunc("/api/v1/admin/moderation", s.requireAuth(s.handleAdminModeration))
	mux.HandleFunc("/api/v1/devices/fleet", s.requireAuth(s.handleDeviceFleet))
	mux.HandleFunc("/api/v1/me", s.requireAuth(s.handleMe))
	mux.HandleFunc("/api/v1/notifications", s.requireAuth(s.handleNotifications))
	mux.HandleFunc("/api/v1/policy/effective", s.requireAuth(s.handleEffectivePolicy))
	mux.HandleFunc("/api/v1/policy/simulations", s.requireAuth(s.handlePolicySimulations))
	mux.HandleFunc("/api/v1/sync/", s.requireAuth(s.handleSyncState))
	mux.HandleFunc("/api/v1/mods", s.handleMods)
	mux.HandleFunc("/api/v1/mods/", s.handleModByID)
	mux.HandleFunc("/api/v1/jobs/", s.requireAuth(s.handleJob))
	mux.HandleFunc("/api/v1/crash-reports", s.requireAuth(s.handleCrashReports))
	mux.HandleFunc("/api/v1/installs", s.requireAuth(s.handleAcceptedPlaceholder("install queued")))
	mux.HandleFunc("/api/v1/install-queue", s.handleInstallQueue)
	mux.HandleFunc("/api/v1/install-pipeline", s.handleInstallPipeline)
	mux.HandleFunc("/api/v1/modpacks", s.requireAuth(s.handleAcceptedPlaceholder("modpack request accepted")))
	mux.HandleFunc("/api/v1/modpacks/cloud", s.handleCloudModpacks)
	mux.HandleFunc("/api/v1/dependency-graph", s.handleDependencyGraph)
	mux.HandleFunc("/api/v1/build-farm/jobs", s.handleBuildFarmJobs)
	mux.HandleFunc("/api/v1/publishers/dashboard", s.requireAuth(s.handlePublisherDashboard))
	mux.HandleFunc("/api/v1/publishers/analytics", s.requireAuth(s.handlePublisherAnalytics))
	mux.HandleFunc("/api/v1/recovery/plan", s.handleRecoveryPlan)
	mux.HandleFunc("/api/v1/releases/desktop", s.handleDesktopReleases)
	mux.HandleFunc("/api/v1/registry/health", s.handleRegistryHealth)
	mux.HandleFunc("/api/v1/registries/federation", s.handleRegistryFederation)
	mux.HandleFunc("/api/v1/trust/reputation", s.handleTrustReputation)
	mux.HandleFunc("/api/v1/workflows/rules", s.handleWorkflowRules)
	mux.HandleFunc("/api/v1/reports", s.requireAuth(s.handleAcceptedPlaceholder("report accepted")))
	mux.HandleFunc("/api/v1/docs-hub", s.handleDocsHub)
	mux.HandleFunc("/api/v1/marketplace/collections", s.handleCollections)
	mux.HandleFunc("/api/v1/compatibility/lab", s.handleCompatibilityLab)
	mux.HandleFunc("/api/v1/compatibility/signals", s.handleCompatibilitySignals)
	mux.HandleFunc("/api/v1/ai/compatibility", s.handleAICompatibilityAssistant)
	mux.HandleFunc("/api/v1/packages/diff", s.handlePackageDiff)
	mux.HandleFunc("/api/v1/platform/major-upgrades", s.handleMajorPlatformUpgrades)
	mux.HandleFunc("/api/v1/platform/product-upgrades", s.handleProductUpgrades)
	return withCORS(mux)
}

func (s *Server) handleRoot(w http.ResponseWriter, r *http.Request) {
	if r.URL.Path != "/" {
		writeJSON(w, http.StatusNotFound, map[string]string{"error": "route not found"})
		return
	}

	writeJSON(w, http.StatusOK, map[string]any{
		"service": "theunlocker-go-api",
		"routes": []string{
			"GET /health",
			"GET /openapi.json",
			"GET /docs",
			"GET /api/v1/health",
			"POST /api/v1/auth/register",
			"POST /api/v1/auth/login",
			"POST /api/v1/auth/refresh",
			"POST /api/v1/auth/logout",
			"POST /api/v1/auth/password-reset/request",
			"POST /api/v1/auth/password-reset/confirm",
			"POST /api/v1/auth/email-verification/request",
			"POST /api/v1/auth/email-verification/confirm",
			"GET /api/v1/me",
			"GET /api/v1/sync/{userId}",
			"GET /api/v1/account/settings",
			"POST /api/v1/account/settings",
			"GET /api/v1/account/security",
			"GET /api/v1/admin/moderation",
			"GET /api/v1/devices/fleet",
			"GET /api/v1/mods",
			"GET /api/v1/mods/{id}",
			"POST /api/v1/jobs/{type}",
			"POST /api/v1/crash-reports",
			"POST /api/v1/installs",
			"GET /api/v1/install-queue",
			"GET /api/v1/install-pipeline",
			"POST /api/v1/modpacks",
			"GET /api/v1/modpacks/cloud",
			"GET /api/v1/dependency-graph",
			"GET /api/v1/build-farm/jobs",
			"GET /api/v1/publishers/dashboard",
			"GET /api/v1/publishers/analytics",
			"GET /api/v1/recovery/plan",
			"GET /api/v1/releases/desktop",
			"GET /api/v1/registry/health",
			"GET /api/v1/registries/federation",
			"GET /api/v1/trust/reputation",
			"GET /api/v1/workflows/rules",
			"GET /api/v1/notifications",
			"GET /api/v1/policy/effective",
			"GET /api/v1/policy/simulations",
			"POST /api/v1/reports",
			"GET /api/v1/docs-hub",
			"GET /api/v1/marketplace/collections",
			"GET /api/v1/compatibility/lab",
			"GET /api/v1/compatibility/signals",
			"GET /api/v1/ai/compatibility",
			"GET /api/v1/packages/diff",
			"GET /api/v1/platform/major-upgrades",
			"GET /api/v1/platform/product-upgrades",
		},
	})
}

func (s *Server) handleDocs(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodGet {
		writeJSON(w, http.StatusMethodNotAllowed, map[string]string{"error": "method not allowed"})
		return
	}

	w.Header().Set("Content-Type", "text/html; charset=utf-8")
	w.WriteHeader(http.StatusOK)
	_, _ = w.Write([]byte(`<!doctype html>
<html>
<head>
  <title>TheUnlocker Go API</title>
  <meta name="viewport" content="width=device-width,initial-scale=1" />
  <style>body{font-family:system-ui,sans-serif;margin:40px;line-height:1.55;color:#172033}code{background:#eef2f7;padding:2px 5px;border-radius:4px}a{color:#1d4ed8}</style>
</head>
<body>
  <h1>TheUnlocker Go API</h1>
  <p>This service is the public versioned API gateway. Use <code>/openapi.json</code> for generated clients or import it into Swagger UI, Scalar, Postman, or Insomnia.</p>
  <p><a href="/openapi.json">Open OpenAPI JSON</a></p>
</body>
</html>`))
}

func (s *Server) handleOpenAPI(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodGet {
		writeJSON(w, http.StatusMethodNotAllowed, map[string]string{"error": "method not allowed"})
		return
	}
	writeJSON(w, http.StatusOK, openAPISpec())
}

func (s *Server) handleHealth(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodGet {
		writeJSON(w, http.StatusMethodNotAllowed, map[string]string{"error": "method not allowed"})
		return
	}

	now := time.Now().UTC()
	writeJSON(w, http.StatusOK, healthResponse{
		Status:       "Healthy",
		Service:      "go-api",
		RegistryBase: s.registryBaseURL,
		StartedAt:    s.startedAt.Format(time.RFC3339),
		CheckedAt:    now.Format(time.RFC3339),
	})
}

func (s *Server) handleMods(w http.ResponseWriter, r *http.Request) {
	if r.Method == http.MethodPost {
		s.requireAuth(s.handleModUpsert)(w, r)
		return
	}
	if r.Method != http.MethodGet {
		writeJSON(w, http.StatusMethodNotAllowed, map[string]string{"error": "method not allowed"})
		return
	}

	if s.modStore != nil {
		mods, err := s.modStore.ListMods(r.Context())
		if err == nil {
			writeJSON(w, http.StatusOK, mods)
			return
		}
	}

	target := "/mods"
	if r.URL.RawQuery != "" {
		target += "?" + r.URL.RawQuery
	}

	if s.proxy(w, r, http.MethodGet, target, nil) == nil {
		return
	}

	writeJSON(w, http.StatusOK, sampleMods())
}

func (s *Server) handleModUpsert(w http.ResponseWriter, r *http.Request) {
	if s.modStore == nil {
		writeJSON(w, http.StatusServiceUnavailable, map[string]string{"error": "mongo mod store is not configured"})
		return
	}

	var mod registryMod
	if err := json.NewDecoder(r.Body).Decode(&mod); err != nil {
		writeJSON(w, http.StatusBadRequest, map[string]string{"error": "invalid mod payload"})
		return
	}
	if strings.TrimSpace(mod.ID) == "" {
		writeJSON(w, http.StatusBadRequest, map[string]string{"error": "mod id is required"})
		return
	}
	if err := s.modStore.UpsertMod(r.Context(), mod); err != nil {
		writeJSON(w, http.StatusInternalServerError, map[string]string{"error": "could not save mod"})
		return
	}
	writeJSON(w, http.StatusCreated, mod)
}

func (s *Server) handleAuthProxy(w http.ResponseWriter, r *http.Request) {
	route := strings.TrimPrefix(r.URL.Path, "/api/v1/auth/")
	if route == "" || strings.Contains(route, "..") {
		writeJSON(w, http.StatusNotFound, map[string]string{"error": "auth route not found"})
		return
	}

	body, err := io.ReadAll(r.Body)
	if err != nil {
		writeJSON(w, http.StatusBadRequest, map[string]string{"error": "could not read request body"})
		return
	}

	if s.proxy(w, r, r.Method, "/auth/"+strings.Trim(route, "/"), body) == nil {
		return
	}

	writeJSON(w, http.StatusServiceUnavailable, map[string]string{"error": "auth registry unavailable"})
}

func (s *Server) handleAccountSettings(w http.ResponseWriter, r *http.Request) {
	body, err := io.ReadAll(r.Body)
	if err != nil {
		writeJSON(w, http.StatusBadRequest, map[string]string{"error": "could not read request body"})
		return
	}

	if s.proxy(w, r, r.Method, "/account/settings", body) == nil {
		return
	}

	writeJSON(w, http.StatusServiceUnavailable, map[string]string{"error": "account settings unavailable"})
}

func (s *Server) handleAccountSecurity(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodGet {
		writeJSON(w, http.StatusMethodNotAllowed, map[string]string{"error": "method not allowed"})
		return
	}

	if s.proxy(w, r, http.MethodGet, "/account/security", nil) == nil {
		return
	}

	writeJSON(w, http.StatusOK, map[string]any{
		"emailVerified":  false,
		"trustedDevices": []string{"Current browser session"},
		"sessions": []map[string]any{
			{"id": "local-fallback", "createdAt": time.Now().UTC().Format(time.RFC3339), "expiresAt": time.Now().UTC().Add(24 * time.Hour).Format(time.RFC3339), "revoked": false},
		},
		"loginAudit": []map[string]any{},
	})
}

func (s *Server) handleAdminModeration(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodGet {
		writeJSON(w, http.StatusMethodNotAllowed, map[string]string{"error": "method not allowed"})
		return
	}

	writeJSON(w, http.StatusOK, []map[string]any{
		{
			"id":          "upload-better-ui-1-3-1",
			"packageId":   "better-ui",
			"publisher":   "studio-official",
			"status":      "scan-pending",
			"riskScore":   18,
			"flags":       []string{"new permission: AddMenuItems", "signed publisher"},
			"submittedAt": time.Now().UTC().Add(-42 * time.Minute).Format(time.RFC3339),
		},
		{
			"id":          "upload-debug-tools-0-9-0",
			"packageId":   "debug-tools",
			"publisher":   "local-dev",
			"status":      "needs-review",
			"riskScore":   71,
			"flags":       []string{"unsigned", "network permission", "packed binary"},
			"submittedAt": time.Now().UTC().Add(-3 * time.Hour).Format(time.RFC3339),
		},
	})
}

func (s *Server) handleDeviceFleet(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodGet {
		writeJSON(w, http.StatusMethodNotAllowed, map[string]string{"error": "method not allowed"})
		return
	}

	writeJSON(w, http.StatusOK, map[string]any{
		"accountId":        "demo-user",
		"onlineDevices":    2,
		"pendingCommands":  3,
		"lastFleetSyncAt":  time.Now().UTC().Format(time.RFC3339),
		"orchestrationKey": "demo-fleet-ed25519",
		"devices": []map[string]any{
			{
				"id":                 "desktop-main",
				"name":               "Main gaming PC",
				"kind":               "WindowsDesktop",
				"status":             "online",
				"lastSeenAt":         time.Now().UTC().Add(-2 * time.Minute).Format(time.RFC3339),
				"registryUrl":        "https://registry.theunlocker.local",
				"activeProfile":      "Vanilla+",
				"installedMods":      42,
				"trustLevel":         "TrustedDevice",
				"canReceiveCommands": true,
				"lanAddress":         "192.168.1.42",
				"pendingCommands": []map[string]string{
					{"id": "cmd-install-better-ui", "type": "install", "status": "queued", "target": "better-ui@1.4.0"},
					{"id": "cmd-sync-policy", "type": "policy-sync", "status": "ready", "target": "enterprise-policy"},
				},
			},
			{
				"id":                 "steamdeck-living-room",
				"name":               "Living room handheld",
				"kind":               "PortableClient",
				"status":             "online",
				"lastSeenAt":         time.Now().UTC().Add(-8 * time.Minute).Format(time.RFC3339),
				"registryUrl":        "https://registry.theunlocker.local",
				"activeProfile":      "Travel",
				"installedMods":      18,
				"trustLevel":         "TrustedDevice",
				"canReceiveCommands": true,
				"lanAddress":         "192.168.1.73",
				"pendingCommands": []map[string]string{
					{"id": "cmd-install-pack-vanilla-plus", "type": "install-pack", "status": "waiting-for-user", "target": "vanilla-plus"},
				},
			},
			{
				"id":                 "lab-vm",
				"name":               "Compatibility lab VM",
				"kind":               "SandboxRunner",
				"status":             "offline",
				"lastSeenAt":         time.Now().UTC().Add(-3 * time.Hour).Format(time.RFC3339),
				"registryUrl":        "http://localhost:4567",
				"activeProfile":      "Testing",
				"installedMods":      7,
				"trustLevel":         "LocalDeveloper",
				"canReceiveCommands": false,
				"lanAddress":         "",
				"pendingCommands":    []map[string]string{},
			},
		},
	})
}

func (s *Server) handleNotifications(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodGet {
		writeJSON(w, http.StatusMethodNotAllowed, map[string]string{"error": "method not allowed"})
		return
	}

	writeJSON(w, http.StatusOK, []map[string]string{
		{"id": "policy-sync", "severity": "info", "title": "Policy synced", "body": "Enterprise policy was refreshed from the registry.", "createdAt": time.Now().UTC().Add(-8 * time.Minute).Format(time.RFC3339)},
		{"id": "permission-diff", "severity": "warning", "title": "Permission approval needed", "body": "Better UI 1.3.1 requests a new AddMenuItems permission.", "createdAt": time.Now().UTC().Add(-23 * time.Minute).Format(time.RFC3339)},
		{"id": "crash-recovery", "severity": "critical", "title": "Crash recovery available", "body": "A previous launch failed. Safe mode and rollback steps are ready.", "createdAt": time.Now().UTC().Add(-1 * time.Hour).Format(time.RFC3339)},
	})
}

func (s *Server) handleEffectivePolicy(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodGet {
		writeJSON(w, http.StatusMethodNotAllowed, map[string]string{"error": "method not allowed"})
		return
	}

	writeJSON(w, http.StatusOK, map[string]any{
		"source":              "team-registry",
		"version":             "2026.05.03",
		"allowUnsignedMods":   false,
		"requiredTrustLevel":  "TrustedPublisher",
		"allowedRegistries":   []string{"official", "studio-private", "local-dev"},
		"blockedPermissions":  []string{"NetworkAccessWithoutConsent", "ArbitraryFileWrite"},
		"blockedMods":         []string{"bad-mod"},
		"lastSyncedAt":        time.Now().UTC().Format(time.RFC3339),
		"nextSyncRecommended": time.Now().UTC().Add(6 * time.Hour).Format(time.RFC3339),
	})
}

func (s *Server) handlePolicySimulations(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodGet {
		writeJSON(w, http.StatusMethodNotAllowed, map[string]string{"error": "method not allowed"})
		return
	}

	now := time.Now().UTC()
	writeJSON(w, http.StatusOK, map[string]any{
		"policyVersion":   "2026.05.04-enterprise",
		"environment":     "studio-private",
		"generatedAt":     now.Format(time.RFC3339),
		"overallDecision": "review",
		"allowedCount":    3,
		"reviewCount":     2,
		"blockedCount":    2,
		"rules": []map[string]any{
			{"id": "signature-required", "label": "Require trusted signatures", "enabled": true, "severity": "block", "description": "Unsigned packages from non-local registries are blocked before install."},
			{"id": "network-consent", "label": "Network permission consent", "enabled": true, "severity": "review", "description": "Any new NetworkAccess permission requires user approval and audit logging."},
			{"id": "stable-ring-default", "label": "Stable ring default", "enabled": true, "severity": "review", "description": "Beta and nightly releases remain disabled unless a profile opts into them."},
			{"id": "registry-allowlist", "label": "Registry allowlist", "enabled": true, "severity": "block", "description": "Installs must come from official, studio-private, or local-dev registries."},
			{"id": "permission-denylist", "label": "Permission denylist", "enabled": true, "severity": "block", "description": "Arbitrary file writes and unmanaged process launch permissions are denied."},
		},
		"scenarios": []map[string]any{
			{
				"id":                   "better-ui-stable",
				"title":                "Better UI stable update",
				"packageId":            "better-ui",
				"version":              "1.4.0",
				"registry":             "official",
				"trustLevel":           "TrustedPublisher",
				"requestedPermissions": []string{"AddMenuItems", "SendNotifications"},
				"updateRing":           "stable",
				"decision":             "allow",
				"score":                94,
				"findings": []map[string]string{
					{"ruleId": "signature-required", "severity": "info", "message": "Publisher signature and public key chain verified."},
					{"ruleId": "stable-ring-default", "severity": "info", "message": "Release is on the stable ring and matches the active profile."},
				},
			},
			{
				"id":                   "debug-tools-local",
				"title":                "Local debug tools",
				"packageId":            "debug-tools",
				"version":              "0.9.0",
				"registry":             "local-dev",
				"trustLevel":           "LocalDeveloper",
				"requestedPermissions": []string{"ReadAssets", "NetworkAccess"},
				"updateRing":           "beta",
				"decision":             "review",
				"score":                54,
				"findings": []map[string]string{
					{"ruleId": "network-consent", "severity": "warning", "message": "NetworkAccess is allowed only after explicit consent."},
					{"ruleId": "stable-ring-default", "severity": "warning", "message": "Beta ring requires profile opt-in."},
				},
			},
			{
				"id":                   "unsigned-ui-community",
				"title":                "Unsigned community UI pack",
				"packageId":            "unsigned-ui-pack",
				"version":              "1.0.0",
				"registry":             "community-unity",
				"trustLevel":           "Unknown",
				"requestedPermissions": []string{"AddMenuItems"},
				"updateRing":           "stable",
				"decision":             "block",
				"score":                18,
				"findings": []map[string]string{
					{"ruleId": "signature-required", "severity": "critical", "message": "Package is unsigned and publisher identity is unknown."},
					{"ruleId": "registry-allowlist", "severity": "critical", "message": "Registry is not allowed by the active enterprise policy."},
				},
			},
			{
				"id":                   "save-backup-helper",
				"title":                "Save backup helper",
				"packageId":            "save-backup-helper",
				"version":              "2.0.0",
				"registry":             "studio-private",
				"trustLevel":           "Official",
				"requestedPermissions": []string{"ReadAssets", "WriteBackups"},
				"updateRing":           "stable",
				"decision":             "allow",
				"score":                97,
				"findings": []map[string]string{
					{"ruleId": "registry-allowlist", "severity": "info", "message": "Package comes from an allowlisted private registry."},
					{"ruleId": "signature-required", "severity": "info", "message": "Official publisher signature verified."},
				},
			},
			{
				"id":                   "nightly-physics-tuner",
				"title":                "Nightly physics tuner",
				"packageId":            "physics-tuner",
				"version":              "3.1.0-nightly.4",
				"registry":             "official",
				"trustLevel":           "TrustedPublisher",
				"requestedPermissions": []string{"RuntimePatching", "AddMenuItems"},
				"updateRing":           "nightly",
				"decision":             "review",
				"score":                61,
				"findings": []map[string]string{
					{"ruleId": "stable-ring-default", "severity": "warning", "message": "Nightly releases require a testing profile."},
					{"ruleId": "permission-denylist", "severity": "warning", "message": "RuntimePatching requires adapter-specific review."},
				},
			},
			{
				"id":                   "unsafe-launcher",
				"title":                "Unsafe external launcher",
				"packageId":            "external-launcher",
				"version":              "1.0.2",
				"registry":             "official",
				"trustLevel":           "TrustedPublisher",
				"requestedPermissions": []string{"LaunchProcess", "ArbitraryFileWrite"},
				"updateRing":           "stable",
				"decision":             "block",
				"score":                12,
				"findings": []map[string]string{
					{"ruleId": "permission-denylist", "severity": "critical", "message": "ArbitraryFileWrite is denied by enterprise policy."},
					{"ruleId": "permission-denylist", "severity": "critical", "message": "LaunchProcess requires a certified adapter extension."},
				},
			},
		},
		"recommendedActions": []string{
			"Approve stable signed updates automatically for official and trusted publishers.",
			"Route local developer and beta packages through permission consent before enabling.",
			"Keep unsigned community packages hidden until publisher verification and signatures are available.",
			"Export blocked simulations into the audit log before rolling out a stricter policy.",
		},
	})
}

func (s *Server) handleOnboarding(w http.ResponseWriter, r *http.Request) {
	body, err := io.ReadAll(r.Body)
	if err != nil {
		writeJSON(w, http.StatusBadRequest, map[string]string{"error": "could not read request body"})
		return
	}

	if s.proxy(w, r, http.MethodPost, "/onboarding", body) == nil {
		return
	}

	writeJSON(w, http.StatusServiceUnavailable, map[string]string{"error": "onboarding registry unavailable"})
}

func (s *Server) handleMe(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodGet {
		writeJSON(w, http.StatusMethodNotAllowed, map[string]string{"error": "method not allowed"})
		return
	}

	if s.proxy(w, r, http.MethodGet, "/auth/session", nil) == nil {
		return
	}

	writeJSON(w, http.StatusServiceUnavailable, map[string]string{"error": "session registry unavailable"})
}

func (s *Server) handleSyncState(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodGet {
		writeJSON(w, http.StatusMethodNotAllowed, map[string]string{"error": "method not allowed"})
		return
	}

	userID := strings.TrimPrefix(r.URL.Path, "/api/v1/sync/")
	if userID == "" || strings.Contains(userID, "/") {
		writeJSON(w, http.StatusNotFound, map[string]string{"error": "sync state not found"})
		return
	}

	if s.proxy(w, r, http.MethodGet, "/sync/"+url.PathEscape(userID), nil) == nil {
		return
	}

	writeJSON(w, http.StatusOK, map[string]any{
		"userId":        userID,
		"installedMods": []string{},
		"favorites":     []string{},
		"profiles":      map[string][]string{},
		"ratings":       map[string]int{},
	})
}

func (s *Server) handleModByID(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodGet {
		writeJSON(w, http.StatusMethodNotAllowed, map[string]string{"error": "method not allowed"})
		return
	}

	id := strings.TrimPrefix(r.URL.Path, "/api/v1/mods/")
	if id == "" || strings.Contains(id, "/") {
		writeJSON(w, http.StatusNotFound, map[string]string{"error": "mod not found"})
		return
	}

	if s.modStore != nil {
		mod, found, err := s.modStore.GetMod(r.Context(), id)
		if err == nil && found {
			writeJSON(w, http.StatusOK, mod)
			return
		}
	}

	if s.proxy(w, r, http.MethodGet, "/mods/"+url.PathEscape(id), nil) == nil {
		return
	}

	for _, mod := range sampleMods() {
		if strings.EqualFold(mod.ID, id) {
			writeJSON(w, http.StatusOK, mod)
			return
		}
	}

	writeJSON(w, http.StatusNotFound, map[string]string{"error": "mod not found"})
}

func (s *Server) handleJob(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodPost {
		writeJSON(w, http.StatusMethodNotAllowed, map[string]string{"error": "method not allowed"})
		return
	}

	jobType := strings.TrimPrefix(r.URL.Path, "/api/v1/jobs/")
	if jobType == "" || strings.Contains(jobType, "/") {
		writeJSON(w, http.StatusBadRequest, map[string]string{"error": "job type is required"})
		return
	}

	body, err := io.ReadAll(r.Body)
	if err != nil {
		writeJSON(w, http.StatusBadRequest, map[string]string{"error": "could not read request body"})
		return
	}

	if s.proxy(w, r, http.MethodPost, "/jobs/"+url.PathEscape(jobType), body) == nil {
		return
	}

	writeJSON(w, http.StatusAccepted, map[string]any{
		"type":      jobType,
		"status":    "accepted-locally",
		"createdAt": time.Now().UTC().Format(time.RFC3339),
	})
}

func (s *Server) handleCrashReports(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodPost {
		writeJSON(w, http.StatusMethodNotAllowed, map[string]string{"error": "method not allowed"})
		return
	}

	body, err := io.ReadAll(r.Body)
	if err != nil {
		writeJSON(w, http.StatusBadRequest, map[string]string{"error": "could not read request body"})
		return
	}

	if s.proxy(w, r, http.MethodPost, "/crash-reports", body) == nil {
		return
	}

	writeJSON(w, http.StatusCreated, map[string]any{
		"status":      "recorded-locally",
		"submittedAt": time.Now().UTC().Format(time.RFC3339),
	})
}

func (s *Server) handleAcceptedPlaceholder(status string) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {
		if r.Method != http.MethodPost {
			writeJSON(w, http.StatusMethodNotAllowed, map[string]string{"error": "method not allowed"})
			return
		}

		writeJSON(w, http.StatusAccepted, map[string]any{
			"status":     status,
			"acceptedAt": time.Now().UTC().Format(time.RFC3339),
		})
	}
}

func (s *Server) handleInstallPipeline(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodGet {
		writeJSON(w, http.StatusMethodNotAllowed, map[string]string{"error": "method not allowed"})
		return
	}

	writeJSON(w, http.StatusOK, []map[string]string{
		{"id": "download", "name": "Download", "status": "ready", "description": "Resolve registry source, select mirror, and stream the package into staging."},
		{"id": "hash", "name": "Hash verify", "status": "ready", "description": "Compare package SHA-256 against version metadata, signed index snapshots, or lockfile pins."},
		{"id": "signature", "name": "Signature verify", "status": "ready", "description": "Validate publisher signatures, trusted public keys, revocation status, and policy requirements."},
		{"id": "scan", "name": "Scan", "status": "waiting", "description": "Run manifest validation, malware adapters, SBOM checks, executable payload rules, and risk scoring."},
		{"id": "dependencies", "name": "Dependency resolve", "status": "ready", "description": "Solve required, optional, peer, SDK, game, runtime, and conflict constraints before install."},
		{"id": "permissions", "name": "Permissions approval", "status": "requires-approval", "description": "Show permission diffs and require approval before enabling new scopes or high-risk capabilities."},
		{"id": "install", "name": "Atomic install", "status": "waiting", "description": "Promote staged files to the active mod directory only after all validation gates pass."},
		{"id": "rollback", "name": "Rollback point", "status": "ready", "description": "Record previous package, manifest, settings, and lockfile so the desktop can restore safely."},
	})
}

func (s *Server) handleInstallQueue(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodGet {
		writeJSON(w, http.StatusMethodNotAllowed, map[string]string{"error": "method not allowed"})
		return
	}

	writeJSON(w, http.StatusOK, []map[string]any{
		{
			"id":           "install-hello-world",
			"packageId":    "hello-world",
			"version":      "1.0.0",
			"status":       "ready-to-install",
			"currentStage": "permissions",
			"progress":     72,
			"rollback":     "available",
			"createdAt":    time.Now().UTC().Add(-12 * time.Minute).Format(time.RFC3339),
		},
		{
			"id":           "update-better-ui",
			"packageId":    "better-ui",
			"version":      "1.3.1",
			"status":       "queued",
			"currentStage": "download",
			"progress":     8,
			"rollback":     "pending",
			"createdAt":    time.Now().UTC().Add(-2 * time.Minute).Format(time.RFC3339),
		},
	})
}

func (s *Server) handleCloudModpacks(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodGet {
		writeJSON(w, http.StatusMethodNotAllowed, map[string]string{"error": "method not allowed"})
		return
	}

	now := time.Now().UTC()
	writeJSON(w, http.StatusOK, map[string]any{
		"featuredCount":      3,
		"sharedInstalls":     18420,
		"immutableLockfiles": 3,
		"lastIndexedAt":      now.Add(-7 * time.Minute).Format(time.RFC3339),
		"modpacks": []map[string]any{
			{
				"id":                  "vanilla-plus",
				"name":                "Vanilla+",
				"version":             "2.3.0",
				"maintainers":         []string{"TheUnlocker Editorial", "Sample Author"},
				"description":         "A conservative starter pack with UI, quality-of-life, and rollback-safe mods.",
				"lockfileUrl":         "https://registry.theunlocker.dev/modpacks/vanilla-plus/2.3.0/unlocker.lock.json",
				"lockfileSha256":      "sha256-demo-vanilla-plus-lock",
				"installUrl":          "theunlocker://install-pack/vanilla-plus",
				"compatibility":       "verified",
				"trustDecision":       "allow",
				"modCount":            12,
				"downloadSizeMb":      148,
				"rollbackVersion":     "2.2.1",
				"updateRing":          "stable",
				"lastCompatibilityAt": now.Add(-2 * time.Hour).Format(time.RFC3339),
				"badges":              []string{"Signed Only", "Low Risk", "Compatibility Lab Passed"},
			},
			{
				"id":                  "creator-lab",
				"name":                "Creator Lab",
				"version":             "0.8.0",
				"maintainers":         []string{"SDK Team"},
				"description":         "Sample mods, panels, event demos, command palette entries, and local development helpers.",
				"lockfileUrl":         "https://registry.theunlocker.dev/modpacks/creator-lab/0.8.0/unlocker.lock.json",
				"lockfileSha256":      "sha256-demo-creator-lab-lock",
				"installUrl":          "theunlocker://install-pack/creator-lab",
				"compatibility":       "watching",
				"trustDecision":       "review",
				"modCount":            18,
				"downloadSizeMb":      221,
				"rollbackVersion":     "0.7.4",
				"updateRing":          "beta",
				"lastCompatibilityAt": now.Add(-34 * time.Minute).Format(time.RFC3339),
				"badges":              []string{"Developer", "Hot Reload", "Beta Ring"},
			},
			{
				"id":                  "experimental-physics-pack",
				"name":                "Experimental Physics Pack",
				"version":             "1.1.0",
				"maintainers":         []string{"Community Physics Group"},
				"description":         "High-risk experimental physics stack with known bridge patch requirements.",
				"lockfileUrl":         "https://registry.theunlocker.dev/modpacks/experimental-physics-pack/1.1.0/unlocker.lock.json",
				"lockfileSha256":      "sha256-demo-experimental-physics-lock",
				"installUrl":          "theunlocker://install-pack/experimental-physics-pack",
				"compatibility":       "failed",
				"trustDecision":       "block",
				"modCount":            9,
				"downloadSizeMb":      312,
				"rollbackVersion":     "1.0.2",
				"updateRing":          "nightly",
				"lastCompatibilityAt": now.Add(-1 * time.Hour).Format(time.RFC3339),
				"badges":              []string{"Bridge Patch Required", "Nightly", "Lab Failed"},
			},
		},
	})
}

func (s *Server) handleDependencyGraph(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodGet {
		writeJSON(w, http.StatusMethodNotAllowed, map[string]string{"error": "method not allowed"})
		return
	}

	writeJSON(w, http.StatusOK, map[string]any{
		"nodes": []map[string]string{
			{"id": "hello-world", "label": "Hello World", "kind": "mod"},
			{"id": "shared-ui-core", "label": "Shared UI Core", "kind": "dependency"},
			{"id": "better-ui", "label": "Better UI", "kind": "mod"},
			{"id": "bridge-ui", "label": "UI Bridge Patch", "kind": "compatibility-patch"},
			{"id": "vanilla-plus", "label": "Vanilla+ Modpack", "kind": "modpack"},
		},
		"edges": []map[string]string{
			{"from": "vanilla-plus", "to": "hello-world", "label": "pins"},
			{"from": "vanilla-plus", "to": "better-ui", "label": "pins"},
			{"from": "hello-world", "to": "shared-ui-core", "label": "requires"},
			{"from": "better-ui", "to": "shared-ui-core", "label": "requires"},
			{"from": "bridge-ui", "to": "hello-world", "label": "patches"},
			{"from": "bridge-ui", "to": "better-ui", "label": "patches"},
		},
	})
}

func (s *Server) handlePublisherDashboard(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodGet {
		writeJSON(w, http.StatusMethodNotAllowed, map[string]string{"error": "method not allowed"})
		return
	}

	writeJSON(w, http.StatusOK, map[string]any{
		"publisherId":       "sample-author",
		"displayName":       "Sample Author",
		"verified":          true,
		"mods":              4,
		"pendingUploads":    2,
		"openCrashReports":  1,
		"monthlyInstalls":   12840,
		"conversionRate":    "18.4%",
		"averageRating":     4.8,
		"signingKeys":       []string{"publisher-key-demo-ed25519", "publisher-key-rotation-next"},
		"moderationStates":  []string{"hello-world approved", "better-ui scan-pending", "debug-tools needs-review"},
		"analyticsSegments": []string{"marketplace page views", "install deep-link clicks", "update adoption", "crash correlation"},
	})
}

func (s *Server) handlePublisherAnalytics(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodGet {
		writeJSON(w, http.StatusMethodNotAllowed, map[string]string{"error": "method not allowed"})
		return
	}

	now := time.Now().UTC()
	writeJSON(w, http.StatusOK, map[string]any{
		"publisherId":      "sample-author",
		"displayName":      "Sample Author",
		"period":           "last-30-days",
		"generatedAt":      now.Format(time.RFC3339),
		"installs":         12840,
		"updates":          9430,
		"marketplaceViews": 69810,
		"installClicks":    12840,
		"conversionRate":   "18.4%",
		"averageRating":    4.8,
		"crashRate":        "0.2%",
		"revenueEstimate":  "$0.00",
		"trend": []map[string]any{
			{"date": now.AddDate(0, 0, -6).Format("2006-01-02"), "installs": 1420, "updates": 990, "crashes": 3, "views": 8120},
			{"date": now.AddDate(0, 0, -5).Format("2006-01-02"), "installs": 1588, "updates": 1180, "crashes": 4, "views": 8740},
			{"date": now.AddDate(0, 0, -4).Format("2006-01-02"), "installs": 1710, "updates": 1324, "crashes": 5, "views": 9210},
			{"date": now.AddDate(0, 0, -3).Format("2006-01-02"), "installs": 1834, "updates": 1410, "crashes": 4, "views": 9960},
			{"date": now.AddDate(0, 0, -2).Format("2006-01-02"), "installs": 2018, "updates": 1512, "crashes": 6, "views": 11040},
			{"date": now.AddDate(0, 0, -1).Format("2006-01-02"), "installs": 2190, "updates": 1650, "crashes": 5, "views": 11920},
		},
		"topMods": []map[string]any{
			{"modId": "better-ui", "name": "Better UI", "installs": 5820, "updates": 4210, "rating": 4.9, "crashRate": "0.1%", "conversionRate": "22.1%"},
			{"modId": "hello-world", "name": "Hello World", "installs": 3860, "updates": 2840, "rating": 4.8, "crashRate": "0.0%", "conversionRate": "19.4%"},
			{"modId": "theme-bridge", "name": "Theme Bridge", "installs": 1960, "updates": 1475, "rating": 4.6, "crashRate": "0.3%", "conversionRate": "14.7%"},
		},
		"funnel": []map[string]any{
			{"stage": "Marketplace views", "count": 69810, "rate": "100%"},
			{"stage": "Detail page opens", "count": 31840, "rate": "45.6%"},
			{"stage": "Install clicks", "count": 12840, "rate": "18.4%"},
			{"stage": "Completed installs", "count": 12190, "rate": "17.5%"},
			{"stage": "Enabled after install", "count": 10940, "rate": "15.7%"},
		},
		"adoption": []map[string]any{
			{"version": "1.4.0", "ring": "stable", "users": 7210, "percentage": "56.2%"},
			{"version": "1.3.1", "ring": "stable", "users": 4380, "percentage": "34.1%"},
			{"version": "1.5.0-beta.1", "ring": "beta", "users": 830, "percentage": "6.5%"},
			{"version": "older", "ring": "legacy", "users": 420, "percentage": "3.2%"},
		},
		"moderationOutcomes": []map[string]any{
			{"status": "approved", "count": 18, "averageHours": 2.4},
			{"status": "scan-pending", "count": 2, "averageHours": 0.6},
			{"status": "needs-review", "count": 1, "averageHours": 7.8},
		},
	})
}

func (s *Server) handleRecoveryPlan(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodGet {
		writeJSON(w, http.StatusMethodNotAllowed, map[string]string{"error": "method not allowed"})
		return
	}

	writeJSON(w, http.StatusOK, []map[string]string{
		{"id": "safe-mode", "label": "Start safe mode", "description": "Disable all mods for the next launch while preserving profiles."},
		{"id": "disable-recent", "label": "Disable recent changes", "description": "Turn off mods installed or updated shortly before the crash."},
		{"id": "rollback", "label": "Rollback updates", "description": "Restore the last healthy package versions from rollback history."},
		{"id": "inspect-logs", "label": "Inspect logs", "description": "Open filtered logs around the last crash timestamp."},
		{"id": "submit-diagnostics", "label": "Submit diagnostics", "description": "Upload config, logs, manifests, health data, and recent errors to the registry."},
	})
}

func (s *Server) handleDesktopReleases(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodGet {
		writeJSON(w, http.StatusMethodNotAllowed, map[string]string{"error": "method not allowed"})
		return
	}

	writeJSON(w, http.StatusOK, map[string]any{
		"currentVersion": "1.1.2",
		"activeChannel":  "stable",
		"autoUpdate":     true,
		"lastCheckedAt":  time.Now().UTC().Format(time.RFC3339),
		"policy": map[string]any{
			"requireSignedUpdates": true,
			"allowPrerelease":      false,
			"rollbackOnFailure":    true,
			"allowedChannels":      []string{"stable", "beta"},
		},
		"releases": []map[string]any{
			{
				"version":           "1.1.2",
				"channel":           "stable",
				"downloadUrl":       "https://updates.theunlocker.local/desktop/1.1.2/TheUnlocker.msix",
				"sha256":            "demo-stable-sha256",
				"signatureUrl":      "https://updates.theunlocker.local/desktop/1.1.2/TheUnlocker.msix.sig",
				"changelog":         "Adds release center, safer update policy display, and rollback health checks.",
				"health":            "healthy",
				"signed":            true,
				"rolloutPercentage": 100,
				"publishedAt":       "2026-05-04T12:00:00Z",
			},
			{
				"version":           "1.2.0-beta.1",
				"channel":           "beta",
				"downloadUrl":       "https://updates.theunlocker.local/desktop/1.2.0-beta.1/TheUnlocker.msix",
				"sha256":            "demo-beta-sha256",
				"signatureUrl":      "https://updates.theunlocker.local/desktop/1.2.0-beta.1/TheUnlocker.msix.sig",
				"changelog":         "Preview build for remote orchestration and cloud profile sync refinements.",
				"health":            "watching",
				"signed":            true,
				"rolloutPercentage": 25,
				"publishedAt":       "2026-05-04T14:30:00Z",
			},
			{
				"version":           "1.3.0-nightly.42",
				"channel":           "nightly",
				"downloadUrl":       "https://updates.theunlocker.local/desktop/nightly/TheUnlocker.msix",
				"sha256":            "demo-nightly-sha256",
				"signatureUrl":      "https://updates.theunlocker.local/desktop/nightly/TheUnlocker.msix.sig",
				"changelog":         "Nightly automation and marketplace experiment channel.",
				"health":            "canary",
				"signed":            true,
				"rolloutPercentage": 5,
				"publishedAt":       "2026-05-04T16:00:00Z",
			},
		},
		"rollback": map[string]any{
			"availableVersion": "1.0.0",
			"lastHealthyAt":    "2026-05-04T11:20:00Z",
			"plan":             "Install signed release, run launch health check, rollback to 1.0.0 if startup fails twice.",
		},
	})
}

func (s *Server) handleRegistryHealth(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodGet {
		writeJSON(w, http.StatusMethodNotAllowed, map[string]string{"error": "method not allowed"})
		return
	}

	writeJSON(w, http.StatusOK, []map[string]any{
		{"service": "go-api", "status": "healthy", "latencyMs": 12, "detail": "public gateway responding"},
		{"service": "ruby-registry", "status": "healthy", "latencyMs": 28, "detail": "auth and marketplace facade available"},
		{"service": "mongo", "status": "healthy", "latencyMs": 9, "detail": "registry database reachable"},
		{"service": "redis", "status": "healthy", "latencyMs": 4, "detail": "job queue accepting work"},
		{"service": "minio", "status": "degraded", "latencyMs": 44, "detail": "object storage reachable; bucket policy pending verification"},
		{"service": "worker", "status": "healthy", "latencyMs": 18, "detail": "last heartbeat received"},
	})
}

func (s *Server) handleRegistryFederation(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodGet {
		writeJSON(w, http.StatusMethodNotAllowed, map[string]string{"error": "method not allowed"})
		return
	}

	now := time.Now().UTC()
	writeJSON(w, http.StatusOK, map[string]any{
		"query":             "ui",
		"connected":         4,
		"healthy":           3,
		"blockedResults":    2,
		"lastFederatedAt":   now.Add(-6 * time.Minute).Format(time.RFC3339),
		"policyVersion":     "team-policy-2026.05",
		"defaultTrustLevel": "TrustedPublisher",
		"registries": []map[string]any{
			{"id": "official", "name": "Official Registry", "url": "https://registry.theunlocker.dev", "kind": "official", "status": "healthy", "trustPolicy": "Official signed publishers only", "priority": 100, "allowUnsigned": false, "packagesIndexed": 1842, "latencyMs": 42, "lastSyncAt": now.Add(-4 * time.Minute).Format(time.RFC3339)},
			{"id": "studio-private", "name": "Studio Private", "url": "https://mods.studio.local", "kind": "private", "status": "healthy", "trustPolicy": "Allowed publishers: studio-official, tools-team", "priority": 80, "allowUnsigned": false, "packagesIndexed": 128, "latencyMs": 19, "lastSyncAt": now.Add(-9 * time.Minute).Format(time.RFC3339)},
			{"id": "local-dev", "name": "Local Developer", "url": "http://localhost:4567", "kind": "local", "status": "healthy", "trustPolicy": "Local developer packages require explicit approval", "priority": 30, "allowUnsigned": true, "packagesIndexed": 17, "latencyMs": 3, "lastSyncAt": now.Add(-1 * time.Minute).Format(time.RFC3339)},
			{"id": "community-unity", "name": "Unity Community", "url": "https://unity-mods.example", "kind": "community", "status": "degraded", "trustPolicy": "Unsigned packages hidden by default", "priority": 50, "allowUnsigned": false, "packagesIndexed": 947, "latencyMs": 211, "lastSyncAt": now.Add(-31 * time.Minute).Format(time.RFC3339)},
		},
		"results": []map[string]any{
			{"packageId": "better-ui", "name": "Better UI", "version": "1.4.0", "registryId": "official", "publisher": "Sample Author", "trustLevel": "TrustedPublisher", "allowedByPolicy": true, "policyDecision": "Signed trusted publisher. Compatible with active policy.", "score": 98},
			{"packageId": "shared-ui-core", "name": "Shared UI Core", "version": "2.1.0", "registryId": "studio-private", "publisher": "studio-official", "trustLevel": "Official", "allowedByPolicy": true, "policyDecision": "Publisher is allowlisted for private registry.", "score": 96},
			{"packageId": "debug-ui-tools", "name": "Debug UI Tools", "version": "0.9.0", "registryId": "local-dev", "publisher": "local-dev", "trustLevel": "LocalDeveloper", "allowedByPolicy": false, "policyDecision": "Requires explicit local developer approval before install.", "score": 51},
			{"packageId": "unsigned-ui-pack", "name": "Unsigned UI Pack", "version": "1.0.0", "registryId": "community-unity", "publisher": "unknown", "trustLevel": "Unknown", "allowedByPolicy": false, "policyDecision": "Unsigned community package hidden by current policy.", "score": 22},
		},
	})
}

func (s *Server) handleTrustReputation(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodGet {
		writeJSON(w, http.StatusMethodNotAllowed, map[string]string{"error": "method not allowed"})
		return
	}

	now := time.Now().UTC()
	writeJSON(w, http.StatusOK, map[string]any{
		"policyVersion":     "team-policy-2026.05",
		"requiredTrust":     "TrustedPublisher",
		"averageScore":      82,
		"flaggedPackages":   2,
		"trustedPublishers": 12,
		"lastEvaluatedAt":   now.Add(-3 * time.Minute).Format(time.RFC3339),
		"packages": []map[string]any{
			{
				"packageId":      "better-ui",
				"name":           "Better UI",
				"publisher":      "Sample Author",
				"version":        "1.4.0",
				"score":          96,
				"trustLevel":     "TrustedPublisher",
				"decision":       "allow",
				"recommendation": "Safe for stable profiles.",
				"positiveFactors": []string{
					"Publisher signature verified",
					"SBOM generated by build farm",
					"No active advisories",
					"Low crash rate",
				},
				"riskFactors": []string{},
			},
			{
				"packageId":      "debug-tools",
				"name":           "Debug Tools",
				"publisher":      "local-dev",
				"version":        "0.9.0",
				"score":          42,
				"trustLevel":     "LocalDeveloper",
				"decision":       "review",
				"recommendation": "Require local developer approval and keep out of stable profiles.",
				"positiveFactors": []string{
					"Manifest validates",
					"SBOM generated",
				},
				"riskFactors": []string{
					"Unsigned binary",
					"Network permission requested",
					"Suspicious imports found during scan",
				},
			},
			{
				"packageId":       "unsigned-ui-pack",
				"name":            "Unsigned UI Pack",
				"publisher":       "unknown",
				"version":         "1.0.0",
				"score":           18,
				"trustLevel":      "Unknown",
				"decision":        "quarantine",
				"recommendation":  "Quarantine until signature, publisher identity, and scan results are available.",
				"positiveFactors": []string{},
				"riskFactors": []string{
					"Unknown publisher",
					"Unsigned package",
					"No provenance attestation",
					"No compatibility results",
				},
			},
		},
		"publishers": []map[string]any{
			{"publisherId": "sample-author", "displayName": "Sample Author", "trustLevel": "TrustedPublisher", "verified": true, "signedReleases": 18, "activeAdvisories": 0, "crashRate": "0.2%", "reputationScore": 94},
			{"publisherId": "studio-official", "displayName": "Studio Official", "trustLevel": "Official", "verified": true, "signedReleases": 44, "activeAdvisories": 0, "crashRate": "0.1%", "reputationScore": 99},
			{"publisherId": "local-dev", "displayName": "Local Developer", "trustLevel": "LocalDeveloper", "verified": false, "signedReleases": 0, "activeAdvisories": 1, "crashRate": "unknown", "reputationScore": 47},
		},
		"advisories": []map[string]any{
			{"id": "ADV-2026-0007", "packageId": "debug-tools", "severity": "medium", "status": "open", "summary": "Network permission added without publisher signature.", "publishedAt": now.Add(-2 * time.Hour).Format(time.RFC3339)},
			{"id": "ADV-2026-0004", "packageId": "legacy-save-tools", "severity": "high", "status": "mitigated", "summary": "Known crash with experimental-physics; bridge patch available.", "publishedAt": now.Add(-36 * time.Hour).Format(time.RFC3339)},
		},
	})
}

func (s *Server) handleWorkflowRules(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodGet {
		writeJSON(w, http.StatusMethodNotAllowed, map[string]string{"error": "method not allowed"})
		return
	}

	writeJSON(w, http.StatusOK, []map[string]any{
		{"id": "backup-before-launch", "trigger": "before-launch", "condition": "profile has save-risk mods", "action": "backup save files", "enabled": true},
		{"id": "disable-after-crash", "trigger": "after-crash", "condition": "same mod blamed twice", "action": "disable mod and mark unsafe", "enabled": true},
		{"id": "stable-ring-only", "trigger": "update-available", "condition": "profile is streaming", "action": "install stable ring only", "enabled": true},
		{"id": "game-update-risk", "trigger": "game-updated", "condition": "SDK compatibility unknown", "action": "disable risky mods until compatibility scan passes", "enabled": false},
	})
}

func (s *Server) handleDocsHub(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodGet {
		writeJSON(w, http.StatusMethodNotAllowed, map[string]string{"error": "method not allowed"})
		return
	}

	writeJSON(w, http.StatusOK, []map[string]string{
		{"title": "SDK Docs", "href": "/SDK.md", "description": "Stable mod interfaces, lifecycle hooks, permissions, services, and compatibility expectations."},
		{"title": "Manifest Schema", "href": "/schemas/mod.schema.json", "description": "JSON Schema for mod.json validation, editor autocomplete, permissions, dependencies, and settings."},
		{"title": "Packaging Guide", "href": "/CLI.md", "description": "CLI flows for init, validate, package, sign, publish, doctor, and CI release artifacts."},
		{"title": "Security Guide", "href": "/SECURITY.md", "description": "Trust policies, signing, quarantine, malware scanning, safe mode, and vulnerability handling."},
		{"title": "Sample Mods", "href": "/examples/README.md", "description": "Buildable examples for menu items, settings, events, themes, panels, permissions, and asset importers."},
		{"title": "Troubleshooting", "href": "/SUPPORT.md", "description": "Recovery center, diagnostics bundles, crash upload, logs, and rollback guidance."},
	})
}

func (s *Server) handleCollections(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodGet {
		writeJSON(w, http.StatusMethodNotAllowed, map[string]string{"error": "method not allowed"})
		return
	}

	writeJSON(w, http.StatusOK, []map[string]any{
		{
			"id":          "unity-starter",
			"name":        "Unity Starter Kit",
			"curator":     "TheUnlocker Editorial",
			"description": "A conservative pack for first-time Unity mod users with signed packages and rollback-safe install gates.",
			"modIds":      []string{"hello-world", "shared-ui-core"},
			"badges":      []string{"Editor Pick", "Signed Only", "Low Risk"},
		},
		{
			"id":          "creator-tools",
			"name":        "Creator Tools",
			"curator":     "SDK Team",
			"description": "Sample mods, panels, command palette extensions, manifest validators, and local development helpers.",
			"modIds":      []string{"command-mod", "settings-mod", "tool-panel-mod"},
			"badges":      []string{"Developer", "Samples", "Hot Reload"},
		},
	})
}

func (s *Server) handleCompatibilitySignals(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodGet {
		writeJSON(w, http.StatusMethodNotAllowed, map[string]string{"error": "method not allowed"})
		return
	}

	writeJSON(w, http.StatusOK, []map[string]any{
		{
			"modA":           "hello-world",
			"modB":           "better-ui",
			"installCount":   1842,
			"crashCount":     3,
			"risk":           "low",
			"recommendation": "Safe together. Load shared-ui-core before both mods.",
		},
		{
			"modA":           "experimental-physics",
			"modB":           "legacy-save-tools",
			"installCount":   219,
			"crashCount":     37,
			"risk":           "high",
			"recommendation": "Use a compatibility bridge or keep one disabled in stable profiles.",
		},
	})
}

func (s *Server) handleCompatibilityLab(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodGet {
		writeJSON(w, http.StatusMethodNotAllowed, map[string]string{"error": "method not allowed"})
		return
	}

	now := time.Now().UTC()
	writeJSON(w, http.StatusOK, map[string]any{
		"activeJobs":      3,
		"queuedJobs":      8,
		"failedJobs":      1,
		"passRate":        "91.4%",
		"adaptersCovered": 3,
		"lastRunAt":       now.Add(-18 * time.Minute).Format(time.RFC3339),
		"queue": map[string]any{
			"queued":         8,
			"running":        3,
			"failed":         1,
			"deadLetter":     0,
			"averageSeconds": 142,
		},
		"adapters": []map[string]any{
			{
				"id":                "unity",
				"name":              "Unity Adapter",
				"status":            "healthy",
				"supportedVersions": []string{"2021 LTS", "2022 LTS", "Unity 6"},
				"lastTestAt":        now.Add(-22 * time.Minute).Format(time.RFC3339),
			},
			{
				"id":                "unreal",
				"name":              "Unreal Adapter",
				"status":            "watching",
				"supportedVersions": []string{"UE 5.3", "UE 5.4"},
				"lastTestAt":        now.Add(-2 * time.Hour).Format(time.RFC3339),
			},
			{
				"id":                "minecraft",
				"name":              "Minecraft Adapter",
				"status":            "healthy",
				"supportedVersions": []string{"Fabric 1.20", "NeoForge 1.21"},
				"lastTestAt":        now.Add(-47 * time.Minute).Format(time.RFC3339),
			},
		},
		"jobs": []map[string]any{
			{
				"id":              "lab-unity-vanilla-plus",
				"modpackId":       "vanilla-plus",
				"gameId":          "unity",
				"adapter":         "unity",
				"status":          "completed",
				"result":          "passed",
				"durationSeconds": 118,
				"startedAt":       now.Add(-31 * time.Minute).Format(time.RFC3339),
				"findings":        []string{"No launch crash", "All declared dependencies satisfied", "FPS delta within budget"},
				"crashSignature":  "",
				"recommendation":  "Safe for stable ring.",
			},
			{
				"id":              "lab-unreal-visual-stack",
				"modpackId":       "visual-stack",
				"gameId":          "unreal",
				"adapter":         "unreal",
				"status":          "running",
				"result":          "pending",
				"durationSeconds": 74,
				"startedAt":       now.Add(-3 * time.Minute).Format(time.RFC3339),
				"findings":        []string{"Sandbox launched", "Waiting on shader warmup trace"},
				"crashSignature":  "",
				"recommendation":  "Hold release decision until adapter completes.",
			},
			{
				"id":              "lab-mc-experimental-physics",
				"modpackId":       "experimental-physics-pack",
				"gameId":          "minecraft",
				"adapter":         "minecraft",
				"status":          "completed",
				"result":          "failed",
				"durationSeconds": 52,
				"startedAt":       now.Add(-1 * time.Hour).Format(time.RFC3339),
				"findings":        []string{"Launch crash reproduced", "Conflict pair matched compatibility signals", "Bridge patch available"},
				"crashSignature":  "NullReference: legacy-save-tools SavePatch.Apply",
				"recommendation":  "Recommend bridge-ui-save patch or disable legacy-save-tools.",
			},
		},
	})
}

func (s *Server) handleBuildFarmJobs(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodGet {
		writeJSON(w, http.StatusMethodNotAllowed, map[string]string{"error": "method not allowed"})
		return
	}

	now := time.Now().UTC()
	writeJSON(w, http.StatusOK, map[string]any{
		"activeJobs":       4,
		"queuedJobs":       12,
		"successfulToday":  38,
		"failedToday":      2,
		"averageBuildTime": "3m 42s",
		"lastCompletedAt":  now.Add(-9 * time.Minute).Format(time.RFC3339),
		"workers": []map[string]any{
			{
				"id":              "worker-win-signing-01",
				"pool":            "windows-signing",
				"status":          "healthy",
				"currentJob":      "build-better-ui-1-4-0",
				"capabilities":    []string{"dotnet", "signing", "sbom", "yara"},
				"lastHeartbeatAt": now.Add(-28 * time.Second).Format(time.RFC3339),
			},
			{
				"id":              "worker-linux-repro-02",
				"pool":            "linux-reproducible",
				"status":          "healthy",
				"currentJob":      "build-shared-ui-core-2-1-0",
				"capabilities":    []string{"go", "rust", "cyclonedx", "clamav"},
				"lastHeartbeatAt": now.Add(-44 * time.Second).Format(time.RFC3339),
			},
		},
		"jobs": []map[string]any{
			{
				"id":                    "build-better-ui-1-4-0",
				"packageId":             "better-ui",
				"version":               "1.4.0",
				"publisher":             "Sample Author",
				"sourceCommit":          "8f31c1b",
				"ciRunUrl":              "https://ci.example/runs/8841",
				"status":                "running",
				"stage":                 "signature",
				"durationSeconds":       183,
				"reproducible":          true,
				"sbomGenerated":         true,
				"signatureVerified":     true,
				"malwareScan":           "clean",
				"provenanceAttested":    true,
				"artifactSha256":        "sha256-demo-better-ui-1-4-0",
				"promotionRing":         "beta",
				"startedAt":             now.Add(-4 * time.Minute).Format(time.RFC3339),
				"releaseRecommendation": "Promote to beta after compatibility lab completes.",
			},
			{
				"id":                    "build-debug-tools-0-9-0",
				"packageId":             "debug-tools",
				"version":               "0.9.0",
				"publisher":             "local-dev",
				"sourceCommit":          "local-dev",
				"ciRunUrl":              "",
				"status":                "failed",
				"stage":                 "malware-scan",
				"durationSeconds":       71,
				"reproducible":          false,
				"sbomGenerated":         true,
				"signatureVerified":     false,
				"malwareScan":           "suspicious-imports",
				"provenanceAttested":    false,
				"artifactSha256":        "sha256-demo-debug-tools-0-9-0",
				"promotionRing":         "blocked",
				"startedAt":             now.Add(-37 * time.Minute).Format(time.RFC3339),
				"releaseRecommendation": "Keep quarantined until publisher signs package and scan flags are reviewed.",
			},
			{
				"id":                    "build-shared-ui-core-2-1-0",
				"packageId":             "shared-ui-core",
				"version":               "2.1.0",
				"publisher":             "Studio Official",
				"sourceCommit":          "4b7e221",
				"ciRunUrl":              "https://ci.example/runs/8838",
				"status":                "completed",
				"stage":                 "published",
				"durationSeconds":       214,
				"reproducible":          true,
				"sbomGenerated":         true,
				"signatureVerified":     true,
				"malwareScan":           "clean",
				"provenanceAttested":    true,
				"artifactSha256":        "sha256-demo-shared-ui-core-2-1-0",
				"promotionRing":         "stable",
				"startedAt":             now.Add(-18 * time.Minute).Format(time.RFC3339),
				"releaseRecommendation": "Published to stable ring.",
			},
		},
	})
}

func (s *Server) handleAICompatibilityAssistant(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodGet {
		writeJSON(w, http.StatusMethodNotAllowed, map[string]string{"error": "method not allowed"})
		return
	}

	now := time.Now().UTC()
	writeJSON(w, http.StatusOK, map[string]any{
		"analysisId":        "ai-compat-vanilla-plus-2026-05",
		"subject":           "Vanilla+ profile with Better UI and Experimental Physics Pack",
		"confidence":        "high",
		"overallRisk":       "medium",
		"generatedAt":       now.Format(time.RFC3339),
		"recommendedAction": "Install bridge-ui-save before enabling Experimental Physics Pack, then keep the pack in beta profile until the lab passes.",
		"summary":           "The selected profile is mostly safe, but one nightly modpack has a repeated crash signature and a missing bridge patch.",
		"suggestions": []map[string]any{
			{
				"id":          "load-order-shared-ui",
				"kind":        "load-order",
				"severity":    "info",
				"title":       "Load shared UI services first",
				"detail":      "Place shared-ui-core before hello-world and better-ui so both mods bind the same SDK menu service.",
				"affectedIds": []string{"shared-ui-core", "hello-world", "better-ui"},
				"actions":     []string{"Move shared-ui-core before UI mods", "Regenerate lockfile", "Run compatibility lab"},
			},
			{
				"id":          "bridge-experimental-physics",
				"kind":        "bridge-patch",
				"severity":    "warning",
				"title":       "Bridge patch recommended",
				"detail":      "experimental-physics-pack and legacy-save-tools share a crash signature that is mitigated by bridge-ui-save.",
				"affectedIds": []string{"experimental-physics-pack", "legacy-save-tools", "bridge-ui-save"},
				"actions":     []string{"Install bridge-ui-save", "Keep pack in beta profile", "Block nightly auto-update"},
			},
			{
				"id":          "network-permission-review",
				"kind":        "permission",
				"severity":    "warning",
				"title":       "Review new network permission",
				"detail":      "debug-tools requests NetworkAccess while remaining unsigned. Require explicit local developer approval.",
				"affectedIds": []string{"debug-tools"},
				"actions":     []string{"Keep disabled in stable profile", "Ask publisher to sign package", "Record permission approval decision"},
			},
			{
				"id":          "migration-settings-1-4",
				"kind":        "migration",
				"severity":    "info",
				"title":       "Settings migration available",
				"detail":      "better-ui 1.4.0 includes a settings migration from 1.3.x that should run before app-ready hooks.",
				"affectedIds": []string{"better-ui"},
				"actions":     []string{"Run migration dry-run", "Back up profile settings", "Promote after successful startup"},
			},
		},
		"evidence": []map[string]string{
			{"source": "compatibility-lab", "signal": "experimental-physics-pack failed with legacy-save-tools crash signature"},
			{"source": "dependency-graph", "signal": "shared-ui-core is required by two active UI mods"},
			{"source": "trust-reputation", "signal": "debug-tools is unsigned and requests network access"},
			{"source": "manifest-migration", "signal": "better-ui declares migration from 1.3.x to 1.4.x"},
		},
	})
}

func (s *Server) handlePackageDiff(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodGet {
		writeJSON(w, http.StatusMethodNotAllowed, map[string]string{"error": "method not allowed"})
		return
	}

	now := time.Now().UTC()
	writeJSON(w, http.StatusOK, map[string]any{
		"packageId":          "better-ui",
		"name":               "Better UI",
		"fromVersion":        "1.3.1",
		"toVersion":          "1.4.0",
		"publisher":          "Sample Author",
		"generatedAt":        now.Format(time.RFC3339),
		"riskChange":         "low-to-medium",
		"decision":           "requires-approval",
		"summary":            "The update is signed and compatible, but adds a menu permission and a settings migration.",
		"permissionChanges":  []map[string]string{{"permission": "AddMenuItems", "change": "added", "reason": "New command palette entry and Tools menu item"}, {"permission": "SendNotifications", "change": "unchanged", "reason": "Existing update notification service"}},
		"dependencyChanges":  []map[string]string{{"dependency": "shared-ui-core", "from": ">=2.0.0 <3.0.0", "to": ">=2.1.0 <3.0.0", "change": "tightened"}, {"dependency": "theme-bridge", "from": "", "to": ">=1.0.0 <2.0.0", "change": "added-optional"}},
		"fileChanges":        []map[string]any{{"path": "BetterUi.dll", "change": "modified", "sha256": "sha256-demo-better-ui-1-4-0", "sizeDeltaKb": 42}, {"path": "assets/menu-icons.zip", "change": "added", "sha256": "sha256-demo-menu-icons", "sizeDeltaKb": 188}, {"path": "mod.json", "change": "modified", "sha256": "sha256-demo-manifest", "sizeDeltaKb": 1}},
		"settingsMigrations": []map[string]string{{"id": "better-ui-settings-1-4", "from": "1.3.x", "to": "1.4.0", "status": "dry-run-ready", "description": "Moves toolbar visibility settings into the new menu service namespace."}},
		"changelog":          []string{"Adds command palette integration.", "Adds optional theme bridge integration.", "Moves toolbar settings to the shared menu namespace."},
		"rollback":           map[string]string{"availableVersion": "1.3.1", "snapshotId": "rollback-better-ui-1-3-1", "strategy": "restore previous DLL, manifest, assets, and settings backup"},
	})
}

func (s *Server) handleMajorPlatformUpgrades(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodGet {
		writeJSON(w, http.StatusMethodNotAllowed, map[string]string{"error": "method not allowed"})
		return
	}

	writeJSON(w, http.StatusOK, []map[string]any{
		{
			"id":          "federation",
			"name":        "Real Multi-Registry Federation",
			"status":      "runtime-ready",
			"description": "Search official, private team, local dev, and game-specific community registries with per-registry trust policy decisions.",
			"surfaces":    []string{"desktop", "go-api", "runtime"},
		},
		{
			"id":          "hosted-build-farm",
			"name":        "Hosted Build Farm",
			"status":      "queue-model-ready",
			"description": "Queue reproducible builds with signed artifacts, SBOM generation, malware scans, and provenance attestations.",
			"surfaces":    []string{"registry", "worker", "runtime"},
		},
		{
			"id":          "publisher-economy",
			"name":        "Full Publisher Economy",
			"status":      "domain-ready",
			"description": "Verified publisher pages, donations, paid listings, revenue reports, licensing, team ownership, and collaborator roles.",
			"surfaces":    []string{"marketplace", "registry"},
		},
		{
			"id":          "sat-solver",
			"name":        "Mod Dependency SAT Solver",
			"status":      "runtime-ready",
			"description": "Resolve version ranges, conflicts, optional dependencies, peer dependencies, game/runtime constraints, and lockfile output.",
			"surfaces":    []string{"desktop", "cli", "runtime"},
		},
		{
			"id":          "cloud-sync",
			"name":        "Cloud Profiles And Device Sync",
			"status":      "runtime-ready",
			"description": "Sync installed mods, modpacks, profiles, load orders, favorites, trust decisions, and account settings across devices.",
			"surfaces":    []string{"desktop", "go-api"},
		},
		{
			"id":          "remote-orchestration",
			"name":        "Remote Desktop Orchestration",
			"status":      "domain-ready",
			"description": "Push install commands from the web marketplace to online gaming PCs, Steam Deck-style clients, home servers, and LAN devices.",
			"surfaces":    []string{"marketplace", "desktop"},
		},
		{
			"id":          "compatibility-lab",
			"name":        "Game Runtime Compatibility Lab",
			"status":      "worker-ready",
			"description": "Run adapter sandbox jobs, install modpacks, launch test sessions, collect logs, detect crashes/freezes, and record compatibility.",
			"surfaces":    []string{"worker", "registry"},
		},
		{
			"id":          "workflow-automation",
			"name":        "Visual Workflow Automation",
			"status":      "runtime-ready",
			"description": "Evaluate user rules for game updates, pre-launch save backups, crash diagnostics, and stable-ring update installs.",
			"surfaces":    []string{"desktop", "runtime"},
		},
		{
			"id":          "modpack-products",
			"name":        "First-Class Modpack Ecosystem",
			"status":      "domain-ready",
			"description": "Treat modpacks as maintained products with versions, maintainers, changelogs, screenshots, lockfiles, compatibility matrices, one-click install, and rollback.",
			"surfaces":    []string{"marketplace", "desktop", "registry"},
		},
		{
			"id":          "observability",
			"name":        "Runtime Observability Layer",
			"status":      "runtime-ready",
			"description": "Track load duration, memory delta, handlers, commands, exceptions, crash correlation, and FPS impact where adapters support it.",
			"surfaces":    []string{"desktop", "worker"},
		},
		{
			"id":          "trust-reputation",
			"name":        "Trust And Reputation Engine",
			"status":      "runtime-ready",
			"description": "Score packages using signatures, publisher verification, scans, crash reports, permissions, installs, update history, and reports.",
			"surfaces":    []string{"desktop", "marketplace", "registry"},
		},
		{
			"id":          "policy-as-code",
			"name":        "Policy-As-Code",
			"status":      "runtime-ready",
			"description": "Version team and enterprise rules for unsigned mods, allowed registries, blocked permissions, trust levels, and blocked packages.",
			"surfaces":    []string{"desktop", "enterprise"},
		},
		{
			"id":          "extension-marketplace",
			"name":        "Extension Marketplace For TheUnlocker",
			"status":      "domain-ready",
			"description": "Publish game adapters, UI themes, package format plugins, scanner plugins, marketplace panels, and workflow actions.",
			"surfaces":    []string{"marketplace", "plugins"},
		},
		{
			"id":          "ai-compatibility-assistant",
			"name":        "AI Compatibility Assistant",
			"status":      "analysis-ready",
			"description": "Inspect manifests, logs, crash stacks, dependency graphs, permissions, and targets to suggest load order fixes and migrations.",
			"surfaces":    []string{"desktop", "marketplace"},
		},
		{
			"id":          "desktop-self-updater",
			"name":        "Desktop Self-Updater And Release Channels",
			"status":      "domain-ready",
			"description": "Support stable, beta, and nightly channels with signed updates, changelog UI, release health checks, and automatic rollback planning.",
			"surfaces":    []string{"desktop", "release"},
		},
	})
}

func (s *Server) handleProductUpgrades(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodGet {
		writeJSON(w, http.StatusMethodNotAllowed, map[string]string{"error": "method not allowed"})
		return
	}

	writeJSON(w, http.StatusOK, []map[string]any{
		productUpgrade("real-account-auth", "Real Account Auth", "Identity", "gateway-ready", "Refresh tokens, device sessions, password reset hooks, email verification state, trusted devices, and login audit logs.", []string{"Rotate refresh tokens", "Revoke device sessions", "Record login audit events"}, map[string]string{"Hashing": "bcrypt", "Session TTL": "24h/30d"}),
		productUpgrade("desktop-cloud-sync", "Desktop-to-Cloud Sync", "Sync", "runtime-ready", "Sync installed mods, profiles, favorites, registry settings, trust decisions, and recovery history across machines.", []string{"Pull sync state", "Push profile changes", "Merge trust decisions"}, map[string]string{"Synced types": "6", "Conflict mode": "last-write wins"}),
		productUpgrade("real-install-pipeline", "Real Mod Install Pipeline", "Installation", "workflow-ready", "Stage packages through download, hash verification, signatures, scans, dependency resolution, permissions, atomic install, and rollback.", []string{"Queue install", "Promote staged package", "Create rollback record"}, map[string]string{"Stages": "8", "Rollback": "atomic"}),
		productUpgrade("publisher-portal", "Registry Publisher Portal", "Publishing", "marketplace-ready", "A dashboard for uploads, changelogs, screenshots, signing keys, analytics, crash reports, and moderation status.", []string{"Upload package", "Manage signing keys", "Review crash reports"}, map[string]string{"Portal views": "6", "Signing": "required by policy"}),
		productUpgrade("modpack-studio", "Modpack Studio", "Modpacks", "editor-ready", "Build modpacks with dependency graph preview, lockfile generation, compatibility warnings, export, and share links.", []string{"Generate lockfile", "Preview conflicts", "Export install link"}, map[string]string{"Graph": "live", "Lockfile": "exact pins"}),
		productUpgrade("live-dependency-graph", "Live Dependency Graph", "Compatibility", "marketplace-ready", "Render dependencies, optional integrations, conflicts, bridge patches, and resolved load order as an interactive graph.", []string{"Render mod nodes", "Render conflict edges", "Explain load order"}, map[string]string{"Graph data": "API", "Edge types": "4"}),
		productUpgrade("risk-score-explanations", "Risk Score Explanations", "Trust", "runtime-ready", "Explain risk scores using signatures, permissions, reports, crash data, suspicious imports, and unsigned binaries.", []string{"Show trust factors", "Show risk factors", "Link advisories"}, map[string]string{"Score model": "explainable", "Factors": "7"}),
		productUpgrade("crash-recovery-wizard", "Crash Recovery Wizard", "Recovery", "desktop-ready", "Guide users through safe mode, disabling recent mods, rollback, logs, and diagnostics upload after crashes.", []string{"Enable safe mode", "Disable recent mods", "Submit diagnostics"}, map[string]string{"Recovery paths": "5", "Upload": "registry"}),
		productUpgrade("plugin-marketplace", "Plugin Marketplace", "Extensions", "domain-ready", "Let developers publish game adapters, UI panels, scanners, workflow actions, themes, and package formats.", []string{"Browse adapters", "Install scanner plugin", "Enable UI panel"}, map[string]string{"Extension types": "6", "Trust": "publisher based"}),
		productUpgrade("workflow-automation", "Workflow Automation", "Automation", "runtime-ready", "User-defined rules for save backups, crash handling, game update responses, and stable-ring updates.", []string{"Before launch backup", "Crash disablement rule", "Stable updates only"}, map[string]string{"Rule triggers": "4", "Actions": "policy gated"}),
		productUpgrade("compatibility-intelligence", "Compatibility Intelligence", "Compatibility", "analysis-ready", "Aggregate anonymous install and crash signals to warn about mod combinations that frequently fail together.", []string{"Correlate crashes", "Warn on known bad combos", "Suggest bridge patches"}, map[string]string{"Signals": "crash + install", "Privacy": "anonymous"}),
		productUpgrade("publisher-trust-verification", "Publisher Trust Verification", "Publishing", "policy-ready", "Verify publishers through GitHub orgs, domains, signed releases, trust history, and badges.", []string{"Verify domain", "Verify GitHub org", "Publish trust badge"}, map[string]string{"Badges": "4", "Keys": "rotatable"}),
		productUpgrade("local-developer-mode", "Local Developer Mode", "Developer Experience", "desktop-ready", "Symlink mod projects, rebuild, hot-reload, validate manifests, and stream logs in a local development loop.", []string{"Link project", "Watch builds", "Validate manifest"}, map[string]string{"Reload": "hot", "Links": "symlink/fallback"}),
		productUpgrade("marketplace-collections", "Marketplace Collections", "Discovery", "marketplace-ready", "Curated collections, featured packs, editor picks, game starter packs, and community lists.", []string{"Feature collection", "Share starter pack", "Pin editor picks"}, map[string]string{"Collection types": "5", "Install": "one-click"}),
		productUpgrade("docs-hub", "In-App Documentation Hub", "Documentation", "marketplace-ready", "Surface SDK docs, manifest schema, packaging, signing, sample mods, and troubleshooting inside the app.", []string{"Open SDK docs", "Open manifest schema", "Open troubleshooting"}, map[string]string{"Doc links": "6", "Offline plan": "desktop cache"}),
	})
}

func productUpgrade(id, name, category, status, description string, actions []string, metrics map[string]string) map[string]any {
	rows := make([]map[string]string, 0, len(metrics))
	for label, value := range metrics {
		rows = append(rows, map[string]string{"label": label, "value": value})
	}
	return map[string]any{
		"id":          id,
		"name":        name,
		"category":    category,
		"status":      status,
		"description": description,
		"actions":     actions,
		"metrics":     rows,
	}
}

func (s *Server) requireAuth(next http.HandlerFunc) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {
		if !hasBearerToken(r.Header.Get("Authorization")) && !hasScopedAPIKey(r.Header.Get("X-Api-Key")) {
			writeJSON(w, http.StatusUnauthorized, map[string]string{"error": "authorization required"})
			return
		}

		next(w, r)
	}
}

func hasBearerToken(header string) bool {
	value := strings.TrimSpace(header)
	if value == "" {
		return false
	}

	fields := strings.Fields(value)
	return len(fields) == 2 && strings.EqualFold(fields[0], "Bearer") && len(fields[1]) >= 8
}

func hasScopedAPIKey(header string) bool {
	value := strings.TrimSpace(header)
	return strings.HasPrefix(value, "tu_") && len(value) >= 16
}

func (s *Server) proxy(w http.ResponseWriter, original *http.Request, method string, path string, body []byte) error {
	if s.registryBaseURL == "" {
		return errors.New("registry base url is empty")
	}

	request, err := http.NewRequest(method, s.registryBaseURL+path, bytes.NewReader(body))
	if err != nil {
		return err
	}

	request.Header.Set("Accept", "application/json")
	if len(body) > 0 {
		request.Header.Set("Content-Type", "application/json")
	}
	if auth := original.Header.Get("Authorization"); auth != "" {
		request.Header.Set("Authorization", auth)
	}
	if apiKey := original.Header.Get("X-Api-Key"); apiKey != "" {
		request.Header.Set("X-Api-Key", apiKey)
	}

	response, err := s.httpClient.Do(request)
	if err != nil {
		return err
	}
	defer response.Body.Close()

	responseBody, err := io.ReadAll(response.Body)
	if err != nil {
		return err
	}

	w.Header().Set("Content-Type", "application/json")
	w.WriteHeader(response.StatusCode)
	_, _ = w.Write(responseBody)
	return nil
}

func withCORS(next http.Handler) http.Handler {
	return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		if origin := allowedOrigin(r.Header.Get("Origin")); origin != "" {
			w.Header().Set("Access-Control-Allow-Origin", origin)
			w.Header().Set("Vary", "Origin")
		}
		w.Header().Set("Access-Control-Allow-Methods", "GET,POST,OPTIONS")
		w.Header().Set("Access-Control-Allow-Headers", "Content-Type,X-Api-Key,Authorization")
		w.Header().Set("Content-Security-Policy", "default-src 'none'; frame-ancestors 'none'")
		w.Header().Set("Cross-Origin-Resource-Policy", "same-site")
		w.Header().Set("Referrer-Policy", "no-referrer")
		w.Header().Set("X-Content-Type-Options", "nosniff")
		w.Header().Set("X-Frame-Options", "DENY")
		w.Header().Set("X-Permitted-Cross-Domain-Policies", "none")
		if r.Method == http.MethodOptions {
			w.WriteHeader(http.StatusNoContent)
			return
		}
		next.ServeHTTP(w, r)
	})
}

func allowedOrigin(origin string) string {
	value := strings.TrimSpace(origin)
	switch value {
	case "http://localhost:5173", "http://127.0.0.1:5173", "http://localhost:8080", "http://127.0.0.1:8080":
		return value
	default:
		return ""
	}
}

func writeJSON(w http.ResponseWriter, status int, value any) {
	w.Header().Set("Cache-Control", "no-store")
	w.Header().Set("Content-Type", "application/json")
	w.WriteHeader(status)
	_ = json.NewEncoder(w).Encode(value)
}

func sampleMods() []registryMod {
	return []registryMod{
		{
			ID:          "hello-world",
			Name:        "Hello World",
			Author:      "Sample Author",
			Description: "A safe starter mod served by the Go API fallback.",
			Status:      "Approved",
			GameID:      "unity",
			TrustLevel:  "Trusted Publisher",
			Tags:        []string{"sample", "sdk", "go-api"},
			Permissions: []string{"AddMenuItems", "SendNotifications"},
			Versions: []modVersion{
				{
					Version:     "1.0.0",
					DownloadURL: "#",
					SHA256:      "",
					Changelog:   "Initial release",
					CreatedAt:   time.Now().UTC().Format(time.RFC3339),
				},
			},
		},
	}
}
