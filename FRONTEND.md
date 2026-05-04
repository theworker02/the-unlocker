# Frontends

TheUnlocker has two frontend surfaces.

## Desktop Manager

`TheUnlocker` is the WPF desktop manager. It owns installed mods, enable toggles, marketplace installs, drag-and-drop import, logs, health, conflicts, load order, updates, diagnostics, and `theunlocker://install/<mod-id>` handling.

## Browser Marketplace

`TheUnlocker.Marketplace.Web` is a lightweight marketplace frontend backed by the registry API.

```powershell
dotnet run --project .\TheUnlocker.Marketplace.Web\TheUnlocker.Marketplace.Web.csproj
```

Set `RegistryBaseUrl` to point the marketplace at a hosted registry:

```powershell
$env:RegistryBaseUrl="https://registry.example.com"
```
# Frontend

The primary browser frontend is `frontend/marketplace`.

Stack:

- Vite
- React
- TypeScript
- Nginx for containerized serving

## Authentication And Onboarding

The marketplace starts with account creation/sign-in. Sessions are persisted in `localStorage` and restored through the Ruby registry `/auth/session` endpoint.

The production Nginx config exposes backend lanes:

- `/api/` proxies to the Go API gateway and maps browser-friendly paths to `/api/v1`.
- `/go-api/` proxies to the Go API gateway without path rewriting for generated clients.
- `/ruby-api/` proxies to the Ruby registry facade for internal debugging during migration.

## Routes

The marketplace uses React Router for real browser URLs:

- `/`
- `/mods/:id`
- `/publishers/:id`
- `/publisher-analytics`
- `/policy-lab`
- `/modpacks/:id`
- `/cloud-modpacks`
- `/assistant`
- `/package-diff`
- `/platform`
- `/settings`

The policy lab route renders enterprise rule simulations so operators can preview which package installs would be allowed, sent to review, or blocked before pushing a stricter policy to desktop clients.

Flow:

1. User creates an account or signs in.
2. Ruby registry returns a bearer token and profile.
3. Frontend stores the session in `localStorage`.
4. If onboarding is incomplete, the frontend shows first-run setup.
5. Onboarding saves role, primary game/runtime, and registry URL.
6. Marketplace unlocks after onboarding is complete.

Frontend API calls:

- `POST /auth/register`
- `POST /auth/login`
- `GET /auth/session`
- `POST /auth/logout`
- `POST /onboarding`

## Local Development

```powershell
cd frontend/marketplace
npm install
npm run dev
```

The Vite dev server proxies `/api/*` and `/go-api/*` to the Go API gateway. It also exposes `/ruby-api/*` for debugging the internal Ruby facade during migration work.

## Production Build

```powershell
cd frontend/marketplace
npm run build
```

The Docker image serves the built app with Nginx and proxies `/api/*` and `/go-api/*` to `go-api`. The `/ruby-api/*` path remains available for internal troubleshooting.

## Platform Page

The marketplace includes a platform capability page:

```text
/platform
```

It calls:

```text
/go-api/api/v1/platform/major-upgrades
```

The page displays the major ecosystem modules represented in the runtime and Go API.
