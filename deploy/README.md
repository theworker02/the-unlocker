# Local Deployment

Run the full local platform:

The Compose project is explicitly named `theunlocker`, so local containers use names such as `theunlocker-mongo-1` and `theunlocker-redis-1` instead of inheriting the `deploy` folder name.

```powershell
docker compose -f deploy/docker-compose.yml up --build
```

Run the release-style container stack with the React UI, API gateway, registry services, workers, MongoDB, Redis, and MinIO:

```powershell
docker compose --env-file deploy/release.env.example -f deploy/docker-compose.release.yml up --build -d
```

Release UI:

```text
http://localhost:8080
```

Release details live in [CONTAINER_RELEASE.md](../CONTAINER_RELEASE.md).

Services:

- React frontend: `http://localhost:5173`
- Ruby registry facade: `http://localhost:4567`
- Go API gateway: `http://localhost:8088`
- Rust worker health: `http://localhost:7070/health`
- Registry API: `http://localhost:5077`
- Legacy .NET marketplace: `http://localhost:5080`
- MongoDB: `localhost:27017`
- Redis: `localhost:6379`
- MinIO API: `http://localhost:9000`
- MinIO console: `http://localhost:9001`

The compose profile now separates the browser frontend from the backend:

- `frontend` is Vite + React + TypeScript served through Nginx.
- `ruby-registry` is the web/API facade for marketplace and registry calls.
- `go-api` is the versioned API gateway for clients that should call `/api/v1` routes.
- `rust-worker` is the high-throughput worker lane for package-scan jobs.
- The existing .NET registry/runtime services remain available while the new Ruby/Rust backend is expanded.
