# Major Platform Upgrades

TheUnlocker now includes ecosystem-level building blocks that move it beyond a local mod manager and toward a full modding platform.

## Capability Summary

- Real multi-registry federation.
- Hosted build farm domain services.
- Publisher economy models.
- SAT-style dependency resolution.
- Cloud profile and device sync.
- Remote desktop orchestration.
- Game runtime compatibility lab jobs.
- Visual workflow automation rules.
- First-class modpack products.
- Runtime observability metrics.
- Trust and reputation scoring integration.
- Policy-as-code documents.
- Extension marketplace listings.
- AI compatibility assistant analysis.
- Desktop self-update and release channel planning.

## Runtime Locations

- `TheUnlocker.Modding.Runtime/Registry/FederatedRegistryService.cs`
- `TheUnlocker.Modding.Runtime/BuildFarm/HostedBuildFarm.cs`
- `TheUnlocker.Modding.Runtime/Economy/PublisherEconomy.cs`
- `TheUnlocker.Modding.Runtime/PackageManager/SatDependencySolver.cs`
- `TheUnlocker.Modding.Runtime/Sync/CloudProfileSync.cs`
- `TheUnlocker.Modding.Runtime/Remote/RemoteOrchestration.cs`
- `TheUnlocker.Modding.Runtime/Compatibility/CompatibilityLab.cs`
- `TheUnlocker.Modding.Runtime/Automation/WorkflowAutomation.cs`
- `TheUnlocker.Modding.Runtime/Modpacks/ModpackEcosystem.cs`
- `TheUnlocker.Modding.Runtime/Observability/RuntimeObservability.cs`
- `TheUnlocker.Modding.Runtime/Configuration/PolicyAsCode.cs`
- `TheUnlocker.Modding.Runtime/Extensions/ExtensionMarketplace.cs`
- `TheUnlocker.Modding.Runtime/AI/CompatibilityAssistant.cs`
- `TheUnlocker.Modding.Runtime/Desktop/DesktopSelfUpdater.cs`

## API And UI Surface

The Go API gateway exposes:

```text
GET /api/v1/platform/major-upgrades
```

The React marketplace exposes:

```text
/platform
```

The WPF app exposes:

```text
Major Platform tab
```

## Real Multi-Registry Federation

The federation service searches official registries, private team registries, local development registries, and game-specific community registries. Each result is evaluated against per-registry trust policy before being presented to users.

## Hosted Build Farm

The hosted build farm models queue server-side builds from source or CI metadata. Build jobs can restore source, run reproducible builds, sign artifacts, generate SBOM output, scan packages, and attach provenance attestations before publication.

## Publisher Economy

Publisher economy models support verified publisher pages, donations, paid mods, revenue reports, licensing options, team ownership, and collaborator roles.

## Mod Dependency SAT Solver

The SAT-style solver resolves requested mods against candidate versions, version ranges, conflicts, optional dependencies, peer dependencies, game constraints, SDK/runtime constraints, and lockfile output.

## Cloud Profiles And Device Sync

Cloud profile snapshots sync installed mods, modpacks, game profiles, load order, favorites, trust decisions, ratings, and account settings across devices.

## Remote Desktop Orchestration

Remote orchestration creates install commands for online desktop clients such as gaming PCs, handheld devices, home servers, and LAN-discovered machines.

## Game Runtime Compatibility Lab

Compatibility lab jobs model sandbox test runs that install a modpack, launch a game adapter session, collect logs, detect crash/freeze signals, and record suspected mod combinations.

## Visual Workflow Automation

Workflow automation rules can react to triggers like game updates, pre-launch checks, crashes, and new releases. Actions can disable risky mods, back up saves, export diagnostics, or install stable-ring updates.

## First-Class Modpack Ecosystem

Modpacks are modeled as maintained products with versions, maintainers, changelogs, screenshots, dependency lockfiles, compatibility matrices, one-click install links, and rollback points.

## Runtime Observability Layer

Runtime observability tracks per-mod load duration, memory delta, event handlers, commands, exceptions, last successful load, and FPS impact when adapters support it.

## Trust And Reputation Engine

Trust and reputation scoring combines signature state, publisher verification, package scan results, crash reports, permission risk, install count, update history, and user reports.

## Policy-As-Code

Enterprise and team rules can be represented as JSON:

```json
{
  "allowUnsignedMods": false,
  "allowedRegistries": ["official", "studio-private"],
  "blockedPermissions": ["NetworkAccess"],
  "requiredTrustLevel": "TrustedPublisher",
  "blockedMods": ["bad-mod"]
}
```

## Extension Marketplace

TheUnlocker itself can be extended with published game adapters, UI themes, package format plugins, scanner plugins, marketplace panels, and workflow actions.

## AI Compatibility Assistant

The assistant analyzes manifests, logs, crash stacks, dependency graphs, permissions, and targets to suggest load-order fixes, missing dependencies, unsafe permission warnings, likely conflicts, and migration notes.

## Desktop Self-Updater And Release Channels

The self-updater model supports stable, beta, and nightly channels with signed update metadata, changelog display, release health checks, and rollback planning.

## Verification

Run:

```powershell
dotnet test .\TheUnlocker.Tests\TheUnlocker.Tests.csproj
cd backend/go-api
go test ./...
cd ..\..\frontend\marketplace
npm run build
```
