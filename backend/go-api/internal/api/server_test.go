package api

import (
	"encoding/json"
	"net/http"
	"net/http/httptest"
	"testing"
	"time"
)

func TestHealth(t *testing.T) {
	server := NewServer(Options{
		RegistryBaseURL: "http://registry.test",
		StartedAt:       time.Date(2026, 5, 3, 12, 0, 0, 0, time.UTC),
	})

	request := httptest.NewRequest(http.MethodGet, "/api/v1/health", nil)
	response := httptest.NewRecorder()

	server.Router().ServeHTTP(response, request)

	if response.Code != http.StatusOK {
		t.Fatalf("expected 200, got %d", response.Code)
	}

	var body healthResponse
	if err := json.Unmarshal(response.Body.Bytes(), &body); err != nil {
		t.Fatalf("could not parse response: %v", err)
	}

	if body.Service != "go-api" {
		t.Fatalf("expected go-api service, got %q", body.Service)
	}
}

func TestModsFallback(t *testing.T) {
	server := NewServer(Options{
		RegistryBaseURL: "",
		StartedAt:       time.Date(2026, 5, 3, 12, 0, 0, 0, time.UTC),
	})

	request := httptest.NewRequest(http.MethodGet, "/api/v1/mods", nil)
	response := httptest.NewRecorder()

	server.Router().ServeHTTP(response, request)

	if response.Code != http.StatusOK {
		t.Fatalf("expected 200, got %d", response.Code)
	}

	var mods []registryMod
	if err := json.Unmarshal(response.Body.Bytes(), &mods); err != nil {
		t.Fatalf("could not parse response: %v", err)
	}

	if len(mods) != 1 || mods[0].ID != "hello-world" {
		t.Fatalf("unexpected fallback mods: %+v", mods)
	}
}

func TestModByIDFallback(t *testing.T) {
	server := NewServer(Options{})

	request := httptest.NewRequest(http.MethodGet, "/api/v1/mods/hello-world", nil)
	response := httptest.NewRecorder()

	server.Router().ServeHTTP(response, request)

	if response.Code != http.StatusOK {
		t.Fatalf("expected 200, got %d", response.Code)
	}
}

func TestMissingModByID(t *testing.T) {
	server := NewServer(Options{})

	request := httptest.NewRequest(http.MethodGet, "/api/v1/mods/not-installed", nil)
	response := httptest.NewRecorder()

	server.Router().ServeHTTP(response, request)

	if response.Code != http.StatusNotFound {
		t.Fatalf("expected 404, got %d", response.Code)
	}
}

func TestProductUpgradesFallback(t *testing.T) {
	server := NewServer(Options{})

	request := httptest.NewRequest(http.MethodGet, "/api/v1/platform/product-upgrades", nil)
	response := httptest.NewRecorder()

	server.Router().ServeHTTP(response, request)

	if response.Code != http.StatusOK {
		t.Fatalf("expected 200, got %d", response.Code)
	}

	var body []map[string]any
	if err := json.Unmarshal(response.Body.Bytes(), &body); err != nil {
		t.Fatalf("could not parse response: %v", err)
	}

	if len(body) < 15 {
		t.Fatalf("expected all requested upgrade surfaces, got %d", len(body))
	}
}

func TestInstallPipelineFallback(t *testing.T) {
	server := NewServer(Options{})

	request := httptest.NewRequest(http.MethodGet, "/api/v1/install-pipeline", nil)
	response := httptest.NewRecorder()

	server.Router().ServeHTTP(response, request)

	if response.Code != http.StatusOK {
		t.Fatalf("expected 200, got %d", response.Code)
	}

	var body []map[string]string
	if err := json.Unmarshal(response.Body.Bytes(), &body); err != nil {
		t.Fatalf("could not parse response: %v", err)
	}

	if len(body) != 8 {
		t.Fatalf("expected 8 install stages, got %d", len(body))
	}
}

func TestNestedAuthProxyRouteRequiresRegistry(t *testing.T) {
	server := NewServer(Options{})

	request := httptest.NewRequest(http.MethodPost, "/api/v1/auth/password-reset/request", nil)
	response := httptest.NewRecorder()

	server.Router().ServeHTTP(response, request)

	if response.Code != http.StatusServiceUnavailable {
		t.Fatalf("expected 503 for unavailable auth registry, got %d", response.Code)
	}
}

func TestCollectionsFallback(t *testing.T) {
	server := NewServer(Options{})

	request := httptest.NewRequest(http.MethodGet, "/api/v1/marketplace/collections", nil)
	response := httptest.NewRecorder()

	server.Router().ServeHTTP(response, request)

	if response.Code != http.StatusOK {
		t.Fatalf("expected 200, got %d", response.Code)
	}

	var body []map[string]any
	if err := json.Unmarshal(response.Body.Bytes(), &body); err != nil {
		t.Fatalf("could not parse response: %v", err)
	}
	if len(body) == 0 {
		t.Fatal("expected fallback collections")
	}
}

func TestInstallQueueFallback(t *testing.T) {
	server := NewServer(Options{})

	request := httptest.NewRequest(http.MethodGet, "/api/v1/install-queue", nil)
	response := httptest.NewRecorder()

	server.Router().ServeHTTP(response, request)

	if response.Code != http.StatusOK {
		t.Fatalf("expected 200, got %d", response.Code)
	}

	var body []map[string]any
	if err := json.Unmarshal(response.Body.Bytes(), &body); err != nil {
		t.Fatalf("could not parse response: %v", err)
	}
	if len(body) == 0 {
		t.Fatal("expected fallback install queue items")
	}
}

func TestWorkflowRulesFallback(t *testing.T) {
	server := NewServer(Options{})

	request := httptest.NewRequest(http.MethodGet, "/api/v1/workflows/rules", nil)
	response := httptest.NewRecorder()

	server.Router().ServeHTTP(response, request)

	if response.Code != http.StatusOK {
		t.Fatalf("expected 200, got %d", response.Code)
	}
}

func TestRegistryHealthFallback(t *testing.T) {
	server := NewServer(Options{})

	request := httptest.NewRequest(http.MethodGet, "/api/v1/registry/health", nil)
	response := httptest.NewRecorder()

	server.Router().ServeHTTP(response, request)

	if response.Code != http.StatusOK {
		t.Fatalf("expected 200, got %d", response.Code)
	}
}

func TestDesktopReleasesFallback(t *testing.T) {
	server := NewServer(Options{})

	request := httptest.NewRequest(http.MethodGet, "/api/v1/releases/desktop", nil)
	response := httptest.NewRecorder()

	server.Router().ServeHTTP(response, request)

	if response.Code != http.StatusOK {
		t.Fatalf("expected 200, got %d", response.Code)
	}

	var body map[string]any
	if err := json.Unmarshal(response.Body.Bytes(), &body); err != nil {
		t.Fatalf("could not parse response: %v", err)
	}
	if body["releases"] == nil || body["policy"] == nil || body["rollback"] == nil {
		t.Fatalf("expected releases, policy, and rollback state: %+v", body)
	}
}

func TestDeviceFleetRequiresAuthAndReturnsDevices(t *testing.T) {
	server := NewServer(Options{})

	unauthorized := httptest.NewRecorder()
	server.Router().ServeHTTP(unauthorized, httptest.NewRequest(http.MethodGet, "/api/v1/devices/fleet", nil))
	if unauthorized.Code != http.StatusUnauthorized {
		t.Fatalf("expected 401, got %d", unauthorized.Code)
	}

	request := httptest.NewRequest(http.MethodGet, "/api/v1/devices/fleet", nil)
	request.Header.Set("Authorization", "Bearer test-token")
	response := httptest.NewRecorder()

	server.Router().ServeHTTP(response, request)

	if response.Code != http.StatusOK {
		t.Fatalf("expected 200, got %d", response.Code)
	}

	var body map[string]any
	if err := json.Unmarshal(response.Body.Bytes(), &body); err != nil {
		t.Fatalf("could not parse response: %v", err)
	}
	if body["devices"] == nil || body["orchestrationKey"] == nil {
		t.Fatalf("expected devices and orchestration key: %+v", body)
	}
}

func TestCompatibilityLabFallback(t *testing.T) {
	server := NewServer(Options{})

	request := httptest.NewRequest(http.MethodGet, "/api/v1/compatibility/lab", nil)
	response := httptest.NewRecorder()

	server.Router().ServeHTTP(response, request)

	if response.Code != http.StatusOK {
		t.Fatalf("expected 200, got %d", response.Code)
	}

	var body map[string]any
	if err := json.Unmarshal(response.Body.Bytes(), &body); err != nil {
		t.Fatalf("could not parse response: %v", err)
	}
	if body["adapters"] == nil || body["jobs"] == nil || body["queue"] == nil {
		t.Fatalf("expected adapters, jobs, and queue: %+v", body)
	}
}

func TestBuildFarmJobsFallback(t *testing.T) {
	server := NewServer(Options{})

	request := httptest.NewRequest(http.MethodGet, "/api/v1/build-farm/jobs", nil)
	response := httptest.NewRecorder()

	server.Router().ServeHTTP(response, request)

	if response.Code != http.StatusOK {
		t.Fatalf("expected 200, got %d", response.Code)
	}

	var body map[string]any
	if err := json.Unmarshal(response.Body.Bytes(), &body); err != nil {
		t.Fatalf("could not parse response: %v", err)
	}
	if body["workers"] == nil || body["jobs"] == nil || body["successfulToday"] == nil {
		t.Fatalf("expected workers, jobs, and build metrics: %+v", body)
	}
}

func TestRegistryFederationFallback(t *testing.T) {
	server := NewServer(Options{})

	request := httptest.NewRequest(http.MethodGet, "/api/v1/registries/federation", nil)
	response := httptest.NewRecorder()

	server.Router().ServeHTTP(response, request)

	if response.Code != http.StatusOK {
		t.Fatalf("expected 200, got %d", response.Code)
	}

	var body map[string]any
	if err := json.Unmarshal(response.Body.Bytes(), &body); err != nil {
		t.Fatalf("could not parse response: %v", err)
	}
	if body["registries"] == nil || body["results"] == nil || body["policyVersion"] == nil {
		t.Fatalf("expected federated registries, results, and policy version: %+v", body)
	}
}

func TestTrustReputationFallback(t *testing.T) {
	server := NewServer(Options{})

	request := httptest.NewRequest(http.MethodGet, "/api/v1/trust/reputation", nil)
	response := httptest.NewRecorder()

	server.Router().ServeHTTP(response, request)

	if response.Code != http.StatusOK {
		t.Fatalf("expected 200, got %d", response.Code)
	}

	var body map[string]any
	if err := json.Unmarshal(response.Body.Bytes(), &body); err != nil {
		t.Fatalf("could not parse response: %v", err)
	}
	if body["packages"] == nil || body["publishers"] == nil || body["advisories"] == nil {
		t.Fatalf("expected packages, publishers, and advisories: %+v", body)
	}
}

func TestCloudModpacksFallback(t *testing.T) {
	server := NewServer(Options{})

	request := httptest.NewRequest(http.MethodGet, "/api/v1/modpacks/cloud", nil)
	response := httptest.NewRecorder()

	server.Router().ServeHTTP(response, request)

	if response.Code != http.StatusOK {
		t.Fatalf("expected 200, got %d", response.Code)
	}

	var body map[string]any
	if err := json.Unmarshal(response.Body.Bytes(), &body); err != nil {
		t.Fatalf("could not parse response: %v", err)
	}
	if body["modpacks"] == nil || body["immutableLockfiles"] == nil || body["sharedInstalls"] == nil {
		t.Fatalf("expected modpacks, lockfile count, and shared installs: %+v", body)
	}
}

func TestAICompatibilityAssistantFallback(t *testing.T) {
	server := NewServer(Options{})

	request := httptest.NewRequest(http.MethodGet, "/api/v1/ai/compatibility", nil)
	response := httptest.NewRecorder()

	server.Router().ServeHTTP(response, request)

	if response.Code != http.StatusOK {
		t.Fatalf("expected 200, got %d", response.Code)
	}

	var body map[string]any
	if err := json.Unmarshal(response.Body.Bytes(), &body); err != nil {
		t.Fatalf("could not parse response: %v", err)
	}
	if body["suggestions"] == nil || body["evidence"] == nil || body["recommendedAction"] == nil {
		t.Fatalf("expected suggestions, evidence, and recommendation: %+v", body)
	}
}

func TestPackageDiffFallback(t *testing.T) {
	server := NewServer(Options{})

	request := httptest.NewRequest(http.MethodGet, "/api/v1/packages/diff?packageId=better-ui&from=1.3.1&to=1.4.0", nil)
	response := httptest.NewRecorder()

	server.Router().ServeHTTP(response, request)

	if response.Code != http.StatusOK {
		t.Fatalf("expected 200, got %d", response.Code)
	}

	var body map[string]any
	if err := json.Unmarshal(response.Body.Bytes(), &body); err != nil {
		t.Fatalf("could not parse response: %v", err)
	}
	if body["permissionChanges"] == nil || body["dependencyChanges"] == nil || body["rollback"] == nil {
		t.Fatalf("expected permission changes, dependency changes, and rollback state: %+v", body)
	}
}

func TestPublisherAnalyticsRequiresAuthAndReturnsMetrics(t *testing.T) {
	server := NewServer(Options{})

	unauthorized := httptest.NewRecorder()
	server.Router().ServeHTTP(unauthorized, httptest.NewRequest(http.MethodGet, "/api/v1/publishers/analytics", nil))
	if unauthorized.Code != http.StatusUnauthorized {
		t.Fatalf("expected 401, got %d", unauthorized.Code)
	}

	request := httptest.NewRequest(http.MethodGet, "/api/v1/publishers/analytics", nil)
	request.Header.Set("Authorization", "Bearer test-token")
	response := httptest.NewRecorder()

	server.Router().ServeHTTP(response, request)

	if response.Code != http.StatusOK {
		t.Fatalf("expected 200, got %d", response.Code)
	}

	var body map[string]any
	if err := json.Unmarshal(response.Body.Bytes(), &body); err != nil {
		t.Fatalf("could not parse response: %v", err)
	}
	if body["trend"] == nil || body["funnel"] == nil || body["topMods"] == nil {
		t.Fatalf("expected trend, funnel, and top mods: %+v", body)
	}
}

func TestPolicySimulationsRequireAuthAndReturnDecisions(t *testing.T) {
	server := NewServer(Options{})

	unauthorized := httptest.NewRecorder()
	server.Router().ServeHTTP(unauthorized, httptest.NewRequest(http.MethodGet, "/api/v1/policy/simulations", nil))
	if unauthorized.Code != http.StatusUnauthorized {
		t.Fatalf("expected 401, got %d", unauthorized.Code)
	}

	request := httptest.NewRequest(http.MethodGet, "/api/v1/policy/simulations", nil)
	request.Header.Set("Authorization", "Bearer test-token")
	response := httptest.NewRecorder()

	server.Router().ServeHTTP(response, request)

	if response.Code != http.StatusOK {
		t.Fatalf("expected 200, got %d", response.Code)
	}
	if response.Header().Get("X-Content-Type-Options") != "nosniff" {
		t.Fatalf("expected hardened response headers")
	}

	var body map[string]any
	if err := json.Unmarshal(response.Body.Bytes(), &body); err != nil {
		t.Fatalf("could not parse response: %v", err)
	}
	if body["rules"] == nil || body["scenarios"] == nil || body["recommendedActions"] == nil {
		t.Fatalf("expected rules, scenarios, and recommendations: %+v", body)
	}
}

func TestProtectedRouteRejectsMalformedAuthorizationHeader(t *testing.T) {
	server := NewServer(Options{})
	request := httptest.NewRequest(http.MethodGet, "/api/v1/policy/effective", nil)
	request.Header.Set("Authorization", "test-token-without-bearer")
	response := httptest.NewRecorder()

	server.Router().ServeHTTP(response, request)

	if response.Code != http.StatusUnauthorized {
		t.Fatalf("expected malformed auth header to be rejected, got %d", response.Code)
	}
}

func TestSecurityHeadersAndCorsOriginAllowlist(t *testing.T) {
	server := NewServer(Options{})
	request := httptest.NewRequest(http.MethodGet, "/api/v1/health", nil)
	request.Header.Set("Origin", "https://evil.example")
	response := httptest.NewRecorder()

	server.Router().ServeHTTP(response, request)

	if response.Header().Get("Access-Control-Allow-Origin") != "" {
		t.Fatalf("expected untrusted origin to be omitted")
	}
	if response.Header().Get("Content-Security-Policy") == "" {
		t.Fatalf("expected content security policy header")
	}
	if response.Header().Get("Cache-Control") != "no-store" {
		t.Fatalf("expected no-store cache control")
	}

	allowed := httptest.NewRecorder()
	allowedRequest := httptest.NewRequest(http.MethodOptions, "/api/v1/health", nil)
	allowedRequest.Header.Set("Origin", "http://localhost:5173")
	server.Router().ServeHTTP(allowed, allowedRequest)

	if allowed.Header().Get("Access-Control-Allow-Origin") != "http://localhost:5173" {
		t.Fatalf("expected local dev origin to be allowed")
	}
}

func TestProtectedGovernanceRoutesRequireAuth(t *testing.T) {
	server := NewServer(Options{})

	for _, path := range []string{"/api/v1/admin/moderation", "/api/v1/notifications", "/api/v1/policy/effective", "/api/v1/policy/simulations"} {
		request := httptest.NewRequest(http.MethodGet, path, nil)
		response := httptest.NewRecorder()

		server.Router().ServeHTTP(response, request)

		if response.Code != http.StatusUnauthorized {
			t.Fatalf("expected 401 for %s, got %d", path, response.Code)
		}
	}
}
