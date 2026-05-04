# Database Ownership

TheUnlocker uses multiple backend services, so each persistent collection needs a clear owner.

This prevents Go, Ruby, Rust, and .NET from drifting into competing schemas.

## Ownership Table

| Data area | Owner | Readers | Notes |
| --- | --- | --- | --- |
| Users | Ruby registry facade | Go API gateway, .NET registry | Ruby owns account creation, profile fields, and onboarding persistence for now. |
| Sessions | Ruby registry facade | Go API gateway | Ruby owns bearer tokens, refresh tokens, revocation, and expiry. |
| Login audit | Ruby registry facade | Admin UI, Go API gateway | Records successful and failed sign-in events. |
| Marketplace mods | .NET registry API | Go API gateway, Ruby facade, frontend | .NET remains the canonical registry implementation while gateway migration continues. |
| Package versions | .NET registry API | Go API gateway, Ruby facade, workers | Version history, changelogs, hashes, and package metadata. |
| Package storage objects | Object storage service | Registry APIs, workers | MinIO/S3/Azure store package blobs and screenshots. |
| Worker jobs | Redis | Go API gateway, Ruby facade, Rust worker, .NET worker | API services enqueue jobs; workers consume them. |
| Crash reports | .NET registry API | Go API gateway, Ruby facade, workers | Ruby can intake reports and enqueue triage while .NET stores long-term records. |
| Sync state | .NET registry API | Desktop, Go API gateway | Profiles, installed mods, favorites, ratings, and device sync. |
| Moderation | .NET registry API | Admin UI, Go API gateway | Review queue, flags, advisories, publisher actions. |
| Policies | .NET registry API | Desktop, Go API gateway | Enterprise/team policies and client trust rules. |
| SBOMs and provenance | .NET registry API | Workers, Go API gateway | Generated during package validation and reproducible-build workflows. |
| Reputation/risk scores | .NET registry API | Marketplace, desktop, Go API gateway | Computed from scans, signatures, reports, installs, and trust data. |

## Public API Boundary

The Go API gateway is the public versioned API surface.

Clients should prefer:

```text
/api/v1/...
```

Ruby and .NET remain internal service implementations behind the gateway.

## Migration Direction

1. New public routes start in Go.
2. Go proxies to Ruby or .NET while data ownership remains there.
3. Shared schemas are documented before new collections are introduced.
4. Workers consume Redis jobs but do not mutate unrelated collections directly.
5. Desktop clients call Go APIs for account, marketplace, modpack, install, and report workflows.
