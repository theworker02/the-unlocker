# Containerized Release

This file describes the release-style container stack for The Unlocker.

The stack includes a real browser UI and all core backend lanes:

- React marketplace UI served through Nginx.
- Go public API gateway.
- Ruby registry facade.
- .NET registry API.
- .NET registry worker.
- Rust worker.
- MongoDB.
- Redis.
- MinIO object storage.

## Start The Release Stack

From the repository root:

```powershell
docker compose --env-file deploy/release.env.example -f deploy/docker-compose.release.yml up --build -d
```

Open the UI:

```text
http://localhost:8080
```

The React UI proxies API calls through Nginx:

- `/api/` routes to the Go API gateway at `/api/v1`.
- `/go-api/` routes directly to the Go API gateway.
- `/ruby-api/` routes directly to the Ruby registry facade.

## Service URLs

| Service | URL |
| --- | --- |
| React marketplace UI | `http://localhost:8080` |
| Go API gateway | `http://localhost:8088` |
| Ruby registry facade | `http://localhost:4567` |
| .NET registry API | `http://localhost:5077` |
| Rust worker health | `http://localhost:7070/health` |
| MinIO API | `http://localhost:9000` |
| MinIO console | `http://localhost:9001` |
| MongoDB | `localhost:27017` |
| Redis | `localhost:6379` |

## Optional Legacy UI

The release stack includes the older .NET marketplace UI behind a profile.

```powershell
docker compose --env-file deploy/release.env.example -f deploy/docker-compose.release.yml --profile legacy-ui up --build -d
```

Open:

```text
http://localhost:5080
```

The primary UI remains the React marketplace at `http://localhost:8080`.

## Stop The Stack

```powershell
docker compose -f deploy/docker-compose.release.yml down
```

To also remove local volumes:

```powershell
docker compose -f deploy/docker-compose.release.yml down -v
```

## Release Notes

- The Compose project is named `theunlocker`.
- Fixed container names are used for release clarity.
- The UI container is named `theunlocker-ui`.
- Mongo, Redis, MinIO, and worker report data are stored in Docker volumes.
- Do not use the default example secrets for a public deployment.
- For production hosting, put TLS, real credentials, and external object storage in front of this stack.

## Health Checks

The release stack includes health checks for:

- React UI container.
- Go API gateway.
- Redis.

Additional production health checks can be added for the registry API, registry workers, MinIO bucket readiness, Mongo migrations, and webhook delivery queues.
