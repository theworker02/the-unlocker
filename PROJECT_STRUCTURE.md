# Project Structure

The repository keeps documentation at the root so GitHub surfaces it immediately.

## Applications

```text
TheUnlocker/                  WPF desktop app
frontend/marketplace/         Vite + React + TypeScript marketplace
backend/ruby-registry/        Ruby API facade
backend/go-api/               Go API gateway
backend/rust-worker/          Rust worker service
```

## .NET Platform

```text
TheUnlocker.Modding.Abstractions/
TheUnlocker.Modding.Runtime/
TheUnlocker.Modding.TestHarness/
TheUnlocker.ModPackager/
TheUnlocker.Registry.Server/
TheUnlocker.Registry.Worker/
TheUnlocker.Sdk.Analyzers/
TheUnlocker.Tests/
```

## Game Adapters

```text
TheUnlocker.GameAdapters.Abstractions/
TheUnlocker.Adapter.TestKit/
TheUnlocker.Adapter.Unity/
TheUnlocker.Adapter.Unreal/
TheUnlocker.Adapter.Minecraft/
```

## Authoring Assets

```text
SampleMod/
examples/
templates/
schemas/
```

## Operations

```text
deploy/
.github/
```

## Generated Files

Generated build output is intentionally ignored:

- `bin/`
- `obj/`
- `node_modules/`
- `dist/`
- `target/`
- `packaged-mods/`
- `worker-reports/`
