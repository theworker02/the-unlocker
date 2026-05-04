# Architecture

```text
TheUnlocker/
├── Core/
├── PackageManager/
├── Runtime/
├── RegistryClient/
├── Plugins/
├── GameDetection/
├── CLI/
├── UI/
├── Sandbox/
├── Security/
├── RegistryServer/
├── MarketplaceWeb/
├── Adapters/
```

In this repository those domains are implemented as namespaces/projects:

- `TheUnlocker.Modding.Abstractions`: public mod SDK.
- `TheUnlocker.Modding.Runtime`: package manager, registry client, plugins, game detection, sandbox, security, cooperative runtime hooks, reports.
- `TheUnlocker.ModPackager`: CLI.
- `TheUnlocker`: WPF UI shell.
- `TheUnlocker.Registry.Server`: hosted backend API.
- `TheUnlocker.Marketplace.Web`: browser marketplace UI.
- `TheUnlocker.Adapter.*`: game-specific detection and install conventions.

The runtime is independent from WPF so the desktop app, CLI, registry tooling, tests, and future services can share package and policy behavior.

## Runtime Flow

1. Scan `Mods`.
2. Parse `mod.json`.
3. Validate manifest, policy, permissions, signatures, and dependencies.
4. Resolve load order.
5. Load supported in-process mods through collectible load contexts.
6. Expose services through capability tokens.
7. Track health, logs, telemetry, updates, and diagnostics.

## Platform Data

- `unlocker.lock.json` pins mod versions and package hashes.
- Workspaces describe modpacks and active profiles.
- Compatibility records track game, mod, loader, platform, and conflict status.
- The registry stores version history, moderation states, comments, ratings, crash reports, and user sync state.
