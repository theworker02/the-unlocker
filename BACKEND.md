# Backend

TheUnlocker now has a multi-service backend.

The services are separated by responsibility:

- Go for versioned client-facing APIs.
- Ruby for the registry facade, accounts, sessions, onboarding, and marketplace-friendly endpoints.
- Rust for worker processing and fast package tasks.
- .NET for the established registry API, Swagger, compatibility routes, and platform integration.

See [DATABASE_OWNERSHIP.md](DATABASE_OWNERSHIP.md) for the source-of-truth map across services and collections.

## Service Map

```text
backend/go-api             Go API gateway
backend/ruby-registry      Ruby registry facade
backend/rust-worker        Rust worker service
TheUnlocker.Registry.Server .NET registry API
TheUnlocker.Registry.Worker .NET worker service
```

## Go API Gateway

Path: `backend/go-api`

The Go API gateway provides a stable versioned API layer for desktop, marketplace, CLI, automation, and future external clients.

Responsibilities:

- Versioned API routes under `/api/v1`
- OpenAPI JSON at `/openapi.json`
- Cloud modpack sharing at `GET /api/v1/modpacks/cloud`
- AI compatibility assistant at `GET /api/v1/ai/compatibility`
- Package diff comparison at `GET /api/v1/packages/diff`
- Publisher analytics at `GET /api/v1/publishers/analytics`
- API documentation landing page at `/docs`
- API health checks
- Registry mod list proxying
- Registry mod detail proxying
- Job enqueue proxying
- Crash report proxying
- Authorization header forwarding
- API key header forwarding
- Safe local fallback responses when the registry facade is unavailable

Key endpoints:

- `GET /health`
- `GET /openapi.json`
- `GET /docs`
- `GET /api/v1/health`
- `POST /api/v1/auth/register`
- `POST /api/v1/auth/login`
- `POST /api/v1/auth/refresh`
- `POST /api/v1/auth/logout`
- `GET /api/v1/me`
- `GET /api/v1/sync/{userId}`
- `GET /api/v1/account/settings`
- `POST /api/v1/account/settings`
- `GET /api/v1/mods`
- `GET /api/v1/mods/{id}`
- `POST /api/v1/jobs/{type}`
- `POST /api/v1/crash-reports`
- `GET /api/v1/platform/major-upgrades`

Local run:

```powershell
cd backend/go-api
go test ./...
go run ./cmd/server
```

## Major Platform API

The Go gateway exposes:

```text
GET /api/v1/platform/major-upgrades
```

This returns a shared capability feed for the browser marketplace, desktop app, docs, and future CLI commands. Current entries cover federation, hosted builds, publisher economy, SAT solving, cloud sync, remote orchestration, compatibility lab jobs, workflow automation, modpack products, observability, reputation, policy-as-code, extension marketplace listings, AI compatibility analysis, and desktop release channels.

## Policy Simulation API

The Go gateway also exposes a protected enterprise policy simulation endpoint:

```text
GET /api/v1/policy/simulations
```

This endpoint returns sample rule evaluations, package scenarios, permission findings, update-ring decisions, and recommended rollout actions. It is guarded by the shared auth middleware and now requires either a well-formed bearer token or a scoped TheUnlocker API key.

Security hardening in the Go gateway includes:

- stricter bearer token shape validation
- scoped API key prefix validation
- local-development CORS allowlisting instead of wildcard origins
- `Cache-Control: no-store` on JSON responses
- `Content-Security-Policy: default-src 'none'; frame-ancestors 'none'`
- `X-Content-Type-Options: nosniff`
- `X-Frame-Options: DENY`
- `Referrer-Policy: no-referrer`
- `Cross-Origin-Resource-Policy: same-site`

Environment:

- `PORT` defaults to `8088`
- `REGISTRY_BASE_URL` defaults to `http://ruby-registry:4567`

For local direct execution against a locally running Ruby registry:

```powershell
$env:REGISTRY_BASE_URL = "http://localhost:4567"
$env:PORT = "8088"
go run ./cmd/server
```

Docker URL:

```text
http://localhost:8088
```

## Ruby Registry Facade

Path: `backend/ruby-registry`

The Ruby service is the lightweight registry facade used by the browser marketplace and local stack.

Responsibilities:

- Account creation
- Sign-in/sign-out
- Persistent session tokens
- First-run onboarding persistence
- Marketplace mod search
- Job enqueueing into Redis
- Crash report intake
- MongoDB persistence
- Optional proxy fallback to the .NET registry

Authentication endpoints:

- `POST /auth/register`
- `POST /auth/login`
- `GET /auth/session`
- `POST /auth/logout`
- `POST /onboarding`

Registry endpoints:

- `GET /health`
- `GET /mods`
- `GET /mods/:id`
- `POST /mods`
- `POST /jobs/:type`
- `POST /crash-reports`

Local run:

```powershell
cd backend/ruby-registry
bundle install
bundle exec rackup -o 0.0.0.0 -p 4567
```

Docker URL:

```text
http://localhost:4567
```

## Rust Worker

Path: `backend/rust-worker`

The Rust worker is the high-throughput worker lane for package and scan jobs.

Responsibilities:

- Worker health endpoint
- Redis package scan queue consumption
- Fast file hashing
- Future CPU-heavy scan tasks
- Future compatibility tasks

Local run:

```powershell
cargo check --manifest-path .\backend\rust-worker\Cargo.toml
cargo run --manifest-path .\backend\rust-worker\Cargo.toml
```

Docker URL:

```text
http://localhost:7070/health
```

## .NET Registry API

Path: `TheUnlocker.Registry.Server`

The .NET registry remains available for Swagger, legacy compatibility, and existing registry routes while the Go/Ruby/Rust backend grows.

Responsibilities:

- User account records
- API key issuance
- Mod metadata and version history
- Moderation status and flags
- Ratings and comments
- Crash report ingestion
- User sync state for profiles, favorites, ratings, and installed mods
- MongoDB repository scaffolding
- JSON storage fallback for local development
- Package storage abstractions
- Registry worker integration

Local run:

```powershell
dotnet run --project .\TheUnlocker.Registry.Server\TheUnlocker.Registry.Server.csproj
```

MongoDB configuration:

```powershell
$env:Mongo__RunMigrations = "true"
$env:Mongo__ConnectionString = "mongodb://localhost:27017"
$env:Mongo__DatabaseName = "theunlocker_registry"
dotnet run --project .\TheUnlocker.Registry.Server
```

Docker URL:

```text
http://localhost:5077
```

Key .NET routes:

- `POST /users`
- `POST /auth/api-keys`
- `GET /mods`
- `POST /mods`
- `POST /mods/{id}/versions`
- `POST /mods/{id}/review`
- `POST /mods/{id}/flags`
- `POST /mods/{id}/ratings`
- `POST /mods/{id}/comments`
- `POST /crash-reports`
- `GET /sync/{userId}` internal sync state route
- `POST /sync/{userId}` internal sync state route
- `POST /mods/{id}/packages`
- `POST /mods/{id}/certifications`
- `POST /mods/{id}/provenance`
- `POST /mods/{id}/reproducible-builds`
- `POST /compatibility-tests`
- `GET /admin/review-queue`
- `GET /publishers/{publisherId}/dashboard`

## Jobs And Storage

- Redis queues: `package-scan`, `compatibility-test`, and `reproducible-build`.
- Package storage: local disk, S3-compatible storage, and Azure Blob through `IPackageStorage`.
- Antivirus: ClamAV and YARA adapters through `IMalwareScanner`.

## Docker Compose

Run the local backend stack with:

```powershell
docker compose -f .\deploy\docker-compose.yml up --build
```

Primary backend URLs:

- Go API gateway: `http://localhost:8088`
- Ruby registry facade: `http://localhost:4567`
- Rust worker health: `http://localhost:7070/health`
- .NET registry API: `http://localhost:5077`
- MongoDB: `localhost:27017`
- Redis: `localhost:6379`
- MinIO: `http://localhost:9000`
