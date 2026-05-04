# Stack Direction

TheUnlocker is moving toward a separated frontend/backend architecture.

## Frontend

The primary browser frontend is:

```text
frontend/marketplace
```

Stack:

- Vite
- React
- TypeScript
- Nginx container for production serving

Local commands:

```powershell
cd frontend/marketplace
npm install
npm run dev
npm run build
```

## Backend

The new backend direction is split by responsibility:

```text
backend/ruby-registry
backend/go-api
backend/rust-worker
```

Go handles:

- Versioned API gateway routes
- Public `/api/v1` client API
- OpenAPI output
- Mongo-backed mod reads and writes when configured
- Major platform capability feed at `/api/v1/platform/major-upgrades`
- Health checks for clients and deployments
- Registry facade proxying
- Crash report and job API forwarding
- Future high-throughput API endpoints

Ruby handles:

- Account creation and sign-in
- Persistent session tokens
- Onboarding profile storage
- Marketplace API facade
- Registry-friendly web endpoints
- MongoDB persistence
- Redis job enqueueing
- Compatibility bridge to the existing .NET registry during migration

Rust handles:

- Worker processing
- Package scan jobs
- Fast file hashing and CPU-heavy future tasks
- Redis queue consumption

## Existing .NET Projects

The .NET code remains important:

- WPF desktop app
- SDK abstractions
- mod runtime
- platform runtime services for federation, hosted builds, SAT resolution, cloud sync, policy-as-code, observability, extension marketplace, and self-update channels
- CLI
- tests
- existing registry implementation

The Ruby/Rust services are additive right now. They provide a path to a Docker-native backend while preserving the working .NET mod runtime and desktop tooling.

## Docker

Use:

```powershell
docker compose -f deploy/docker-compose.yml up --build
```

Primary URLs:

- React frontend: `http://localhost:5173`
- Ruby registry: `http://localhost:4567`
- Go API gateway: `http://localhost:8088`
- Rust worker health: `http://localhost:7070/health`
- .NET registry: `http://localhost:5077`
- Legacy .NET marketplace: `http://localhost:5080`
