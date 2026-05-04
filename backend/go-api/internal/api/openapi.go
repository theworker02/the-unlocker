package api

type openAPIDocument struct {
	OpenAPI    string                    `json:"openapi"`
	Info       openAPIInfo               `json:"info"`
	Paths      map[string]map[string]any `json:"paths"`
	Components map[string]any            `json:"components"`
}

type openAPIInfo struct {
	Title       string `json:"title"`
	Version     string `json:"version"`
	Description string `json:"description"`
}

type openAPIBuilder struct {
	document openAPIDocument
}

func newOpenAPIBuilder() *openAPIBuilder {
	return &openAPIBuilder{
		document: openAPIDocument{
			OpenAPI: "3.0.3",
			Info: openAPIInfo{
				Title:       "TheUnlocker Go API",
				Version:     "1.1.2",
				Description: "Versioned public API gateway for TheUnlocker clients.",
			},
			Paths: map[string]map[string]any{},
			Components: map[string]any{
				"securitySchemes": map[string]any{
					"bearerAuth": map[string]any{"type": "http", "scheme": "bearer"},
					"apiKey":     map[string]any{"type": "apiKey", "in": "header", "name": "X-Api-Key"},
				},
			},
		},
	}
}

func (b *openAPIBuilder) route(method, path, summary string, protected bool) *openAPIBuilder {
	if b.document.Paths[path] == nil {
		b.document.Paths[path] = map[string]any{}
	}
	b.document.Paths[path][method] = endpoint(summary, protected)
	return b
}

func (b *openAPIBuilder) build() openAPIDocument {
	return b.document
}

func openAPISpec() openAPIDocument {
	return newOpenAPIBuilder().
		route("get", "/api/v1/health", "Health check", false).
		route("post", "/api/v1/auth/register", "Create account", false).
		route("post", "/api/v1/auth/login", "Sign in", false).
		route("post", "/api/v1/auth/refresh", "Refresh session", false).
		route("post", "/api/v1/auth/logout", "Sign out", true).
		route("post", "/api/v1/auth/password-reset/request", "Request password reset", false).
		route("post", "/api/v1/auth/password-reset/confirm", "Confirm password reset", false).
		route("post", "/api/v1/auth/email-verification/request", "Request email verification", true).
		route("post", "/api/v1/auth/email-verification/confirm", "Confirm email verification", false).
		route("get", "/api/v1/me", "Current user session", true).
		route("get", "/api/v1/sync/{userId}", "Get desktop account sync state", true).
		route("post", "/api/v1/onboarding", "Complete onboarding", true).
		route("get", "/api/v1/account/settings", "Get account settings", true).
		route("post", "/api/v1/account/settings", "Update account settings", true).
		route("get", "/api/v1/account/security", "Get account security state", true).
		route("get", "/api/v1/admin/moderation", "List moderation queue items", true).
		route("get", "/api/v1/devices/fleet", "List account desktop clients, command queues, and orchestration readiness", true).
		route("get", "/api/v1/mods", "List marketplace mods", false).
		route("post", "/api/v1/mods", "Create or update marketplace mod", true).
		route("get", "/api/v1/mods/{id}", "Get marketplace mod details", false).
		route("post", "/api/v1/jobs/{type}", "Create background job", true).
		route("post", "/api/v1/crash-reports", "Submit crash report", true).
		route("post", "/api/v1/installs", "Queue install request", true).
		route("get", "/api/v1/install-queue", "List install and update queue items", false).
		route("get", "/api/v1/install-pipeline", "List staged install pipeline gates", false).
		route("post", "/api/v1/modpacks", "Create or install modpack request", true).
		route("get", "/api/v1/modpacks/cloud", "List cloud-shared modpacks with immutable lockfiles, install links, compatibility status, and rollback metadata", false).
		route("get", "/api/v1/dependency-graph", "Get dependency and compatibility graph preview", false).
		route("get", "/api/v1/build-farm/jobs", "List hosted build farm jobs, workers, scan gates, and provenance outputs", false).
		route("get", "/api/v1/publishers/dashboard", "Get publisher portal dashboard summary", true).
		route("get", "/api/v1/publishers/analytics", "Get publisher analytics for installs, views, conversion, crashes, ratings, adoption, and moderation outcomes", true).
		route("get", "/api/v1/recovery/plan", "List crash recovery wizard steps", false).
		route("get", "/api/v1/releases/desktop", "Get desktop release channels, signed update policy, and rollback state", false).
		route("get", "/api/v1/registry/health", "List detailed registry service health", false).
		route("get", "/api/v1/registries/federation", "List federated registries, policy decisions, and merged search results", false).
		route("get", "/api/v1/trust/reputation", "List package risk scores, publisher reputation, advisories, and trust decisions", false).
		route("get", "/api/v1/workflows/rules", "List workflow automation rules", false).
		route("get", "/api/v1/notifications", "List user notification center items", true).
		route("get", "/api/v1/policy/effective", "Get effective account or enterprise policy", true).
		route("get", "/api/v1/policy/simulations", "Run sample enterprise policy simulations against package permissions, registries, trust levels, and update rings", true).
		route("post", "/api/v1/reports", "Submit report", true).
		route("get", "/api/v1/docs-hub", "List in-app documentation links", false).
		route("get", "/api/v1/marketplace/collections", "List curated marketplace collections", false).
		route("get", "/api/v1/compatibility/lab", "List compatibility lab adapters, queue health, and sandbox job results", false).
		route("get", "/api/v1/compatibility/signals", "List compatibility intelligence signals", false).
		route("get", "/api/v1/ai/compatibility", "List AI compatibility assistant suggestions from manifests, lab results, trust signals, and dependency graphs", false).
		route("get", "/api/v1/packages/diff", "Compare package versions before update, including permission, dependency, file, migration, changelog, and rollback changes", false).
		route("get", "/api/v1/platform/major-upgrades", "List major platform capability modules", false).
		route("get", "/api/v1/platform/product-upgrades", "List implemented product upgrade surfaces", false).
		build()
}

func endpoint(summary string, protected bool) map[string]any {
	result := map[string]any{
		"summary": summary,
		"responses": map[string]any{
			"200": map[string]any{"description": "OK"},
			"202": map[string]any{"description": "Accepted"},
			"401": map[string]any{"description": "Unauthorized"},
		},
	}
	if protected {
		result["security"] = []map[string]any{{"bearerAuth": []string{}}, {"apiKey": []string{}}}
	}
	return result
}
