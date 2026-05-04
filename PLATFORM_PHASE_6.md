# Platform Phase 6

This phase focuses on operational maturity, recovery UX, adapter extensibility, package integrity, and cleaner documentation.

## Deployment

- Added `deploy/docker-compose.yml` for the registry API, worker, MongoDB, Redis, MinIO, and marketplace web app.
- Added Dockerfiles for registry, worker, and marketplace.
- Registry package storage now supports `Storage:Provider = Local|S3|MinIO|Azure`.

## Registry Operations

- Added registry health endpoint with storage provider, Mongo/Redis mode, queue depth, worker heartbeat, and latest scan status.
- Added worker heartbeat endpoint.
- Added background job retry endpoint for dead-lettered jobs.
- Added package scan result records.
- Added configurable moderation scanner rule sets.
- Added SDK compatibility records per mod/version.
- Added publisher verification requests and admin approval decisions.

## Package Safety

- Added package manifest lock validation:
  - verifies `mod.json`
  - verifies declared entry DLL exists
  - checks signature hash metadata
  - warns on executable payloads
  - checks package version against `unlocker.lock.json`
- Added delta update planning from old/new file hash maps.
- Added peer dependency support in `mod.json`.
- Added conflict patch recommendation logic based on shared targets.

## CLI Signing UX

- Existing commands:
  - `unlocker-mod keys`
  - `unlocker-mod sign`
- New commands:
  - `unlocker-mod verify-signature`
  - `unlocker-mod rotate-key`
  - `unlocker-mod revoke-key`

## Desktop UX

- Added Trust Policy tab:
  - unsigned mod policy
  - allowed publishers
  - blocked mods
  - private registries
  - permission defaults
- Added Recovery tab:
  - safe mode
  - disable recently changed mods
  - diagnostics export
  - local development mod link
  - reset policies
- Expanded Risk tab with plain-language permission simulation.

## Adapter Ecosystem

- Added `TheUnlocker.GameAdapters.Abstractions`.
- Added `TheUnlocker.Adapter.TestKit` with fake Unity, Unreal, and Minecraft fixture helpers.

## Documentation

- Documentation is kept at the repository root for GitHub browsing.
- The root `README.md` is the project front door and links into the root documentation set.
- Generated build artifacts are ignored through `.gitignore`.
