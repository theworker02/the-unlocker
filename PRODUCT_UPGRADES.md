# Product Upgrade Surfaces

This file tracks the product-facing platform upgrades that sit on top of the mod runtime, registry, marketplace, and desktop manager.

## Real Account Auth

- Password hashing uses bcrypt in the Ruby registry facade.
- Sessions include access tokens and refresh tokens.
- Session revocation is supported for sign-out and device removal.
- Login audit records capture successful and failed login attempts.
- Trusted devices are part of account settings.
- Password reset request/confirm endpoints are available through the auth gateway.
- Email verification request/confirm endpoints are available through the auth gateway.
- Account security state exposes active/revoked sessions and login audit records.
- API entry points:
  - `POST /api/v1/auth/password-reset/request`
  - `POST /api/v1/auth/password-reset/confirm`
  - `POST /api/v1/auth/email-verification/request`
  - `POST /api/v1/auth/email-verification/confirm`
  - `GET /api/v1/account/security`

## Desktop-to-Cloud Sync

- Sync state includes installed mods, profiles, favorites, registry settings, trust decisions, and recovery history.
- Desktop runtime entry point: `TheUnlocker.Modding.Runtime/Desktop/DesktopAccountSyncService.cs`.
- API entry point: `GET /api/v1/sync/{userId}`.

## Real Mod Install Pipeline

The staged install path is:

1. Download package into staging.
2. Verify package hash.
3. Verify publisher signature.
4. Scan package contents.
5. Resolve dependencies and conflicts.
6. Request permission approval.
7. Install atomically.
8. Record rollback point.

API entry point: `GET /api/v1/install-pipeline`.

## Registry Publisher Portal

Publisher portal data covers:

- package uploads
- changelogs
- screenshots
- signing keys
- analytics
- crash reports
- moderation status

Marketplace entry point: `/operations`.

Publisher dashboard API entry point: `GET /api/v1/publishers/dashboard`.

Marketplace entry point: `/publishers/:id`.

## Publisher Analytics Center

Publisher analytics expands the portal into a real reporting surface:

- install totals
- update totals
- marketplace views
- install click conversion
- average rating
- crash rate
- revenue estimate placeholder
- daily install, update, crash, and view trends
- top mods by installs, updates, rating, crash rate, and conversion
- marketplace funnel stages
- version adoption by release ring
- moderation outcome counts and review duration

Runtime model entry point: `TheUnlocker.Modding.Runtime/Platform/ProductUpgradeServices.cs`.

API entry point: `GET /api/v1/publishers/analytics`.

Marketplace entry point: `/publisher-analytics`.

## Modpack Studio

Modpack Studio supports:

- dependency graph preview
- lockfile generation
- compatibility warnings
- export and share links
- one-click install protocol links

Marketplace entry point: `/modpacks/:id`.

## Cloud Modpack Sharing Center

Cloud modpack sharing treats curated packs as immutable, shareable products:

- stable cloud IDs
- maintainer metadata
- exact lockfile URLs
- lockfile SHA-256 fingerprints
- desktop protocol install links
- compatibility lab status
- trust policy decisions
- package counts
- download size estimates
- rollback versions
- update rings
- badges for signing, risk, lab status, or bridge patch needs

Runtime model entry point: `TheUnlocker.Modding.Runtime/Platform/ProductUpgradeServices.cs`.

API entry point: `GET /api/v1/modpacks/cloud`.

Marketplace entry point: `/cloud-modpacks`.

## Live Dependency Graph

The graph model includes:

- mods
- modpacks
- dependencies
- optional integrations
- compatibility patches
- conflict bridges

API entry point: `GET /api/v1/dependency-graph`.

## Risk Score Explanations

Risk explanations include positive and negative factors:

- trusted publisher signature
- permission risk
- user reports
- recent crash count
- suspicious DLL imports
- unsigned binary payloads
- known advisories

Runtime model: `TheUnlocker.Modding.Runtime/Platform/ProductUpgradeServices.cs`.

## Trust And Reputation Center

The trust center turns those explanations into a single review surface:

- package score
- package decision: allow, review, or quarantine
- positive factors
- risk factors
- publisher reputation
- publisher verification state
- signed release counts
- active advisory counts
- crash rate
- vulnerability advisory summaries
- policy version and required trust level

Runtime model entry point: `TheUnlocker.Modding.Runtime/Platform/ProductUpgradeServices.cs`.

API entry point: `GET /api/v1/trust/reputation`.

Marketplace entry point: `/trust`.

## Crash Recovery Wizard

The recovery flow includes:

- safe mode
- disable recent mods
- rollback updates
- inspect logs
- submit diagnostics

Desktop runtime entry point: `TheUnlocker.Modding.Runtime/Desktop/RecoveryAndDevelopmentServices.cs`.

API entry point: `GET /api/v1/recovery/plan`.

Marketplace entry point: `/control-center`.

## Plugin Marketplace

The plugin marketplace is for The Unlocker extensions, not game mods:

- game adapters
- UI panels
- scanners
- workflow actions
- themes
- package formats

## Workflow Automation

Rule examples:

- before launch, back up save files
- if a mod crashes twice, disable it
- when a game updates, disable risky mods
- only auto-update stable-ring mods

Runtime entry point: `TheUnlocker.Modding.Runtime/Automation/WorkflowAutomation.cs`.

API entry point: `GET /api/v1/workflows/rules`.

Marketplace entry point: `/control-center`.

## Compatibility Intelligence

The compatibility layer aggregates anonymous signals:

- installed mod combinations
- crash counts
- adapter test results
- known conflicts
- recommended bridge patches

Runtime entry point: `TheUnlocker.Modding.Runtime/Wow/CompatibilityIntelligence.cs`.

## Publisher Trust Verification

Verification inputs:

- GitHub organization ownership
- domain ownership
- signed releases
- trusted public keys
- manual admin approval
- trust history

## Local Developer Mode

Developer mode supports:

- symlinked project folders
- rebuild and hot reload loops
- manifest validation
- streamed local logs
- safe local-only trust classification

Runtime entry point: `TheUnlocker.Modding.Runtime/Desktop/RecoveryAndDevelopmentServices.cs`.

## Marketplace Collections

Collections can represent:

- curated lists
- featured packs
- editor picks
- game starter packs
- community lists

API entry point: `GET /api/v1/marketplace/collections`.

Marketplace entry point: `/collections`.

## Compatibility Signals

Compatibility intelligence exposes aggregate signals for mod pairs:

- install count
- crash count
- risk level
- recommendation
- bridge patch suggestion

API entry point: `GET /api/v1/compatibility/signals`.

Marketplace entry point: `/compatibility`.

## AI Compatibility Assistant Center

The assistant surface turns platform metadata into reviewable suggestions:

- load order recommendations
- bridge patch hints
- permission review warnings
- settings migration notes
- affected package IDs
- suggested next actions
- evidence from compatibility lab, dependency graph, trust reputation, and manifest metadata
- confidence and overall risk labels

The assistant is advisory only. It does not patch code, bypass licensing, or override application integrity. Users still approve installs, permissions, and profile changes through normal platform gates.

Runtime model entry point: `TheUnlocker.Modding.Runtime/Platform/ProductUpgradeServices.cs`.

API entry point: `GET /api/v1/ai/compatibility`.

Marketplace entry point: `/assistant`.

## Package Diff Center

The package diff surface compares two versions before a user accepts an update:

- permission additions, removals, and unchanged scopes
- dependency range changes
- optional dependency changes
- file additions and modifications
- SHA-256 fingerprints for changed files
- size deltas
- settings migrations
- changelog entries
- rollback snapshot metadata
- final approval decision
- risk change label

This is the user-facing half of permission diff on update. It gives the desktop and marketplace a consistent update review model before the install pipeline proceeds.

Runtime model entry point: `TheUnlocker.Modding.Runtime/Platform/ProductUpgradeServices.cs`.

API entry point: `GET /api/v1/packages/diff`.

Marketplace entry point: `/package-diff`.

## Compatibility Lab Center

The compatibility lab turns adapter sandbox jobs into an inspectable product surface:

- active, queued, failed, and dead-letter job counts
- average sandbox job duration
- Unity, Unreal, and Minecraft adapter coverage
- supported game/runtime versions per adapter
- recent sandbox job results
- pass/fail/pending release decisions
- reproduced crash signatures
- findings collected during sandbox execution
- release recommendations for stable, beta, or blocked promotion

Runtime model entry point: `TheUnlocker.Modding.Runtime/Platform/ProductUpgradeServices.cs`.

API entry point: `GET /api/v1/compatibility/lab`.

Marketplace entry point: `/lab`.

## Hosted Build Farm Center

The build farm surface models registry-side package builds before marketplace promotion:

- active and queued build jobs
- successful and failed build counts
- worker pools and capabilities
- reproducible build state
- SBOM generation status
- package signature verification
- malware scan decision
- provenance attestation state
- artifact SHA-256 hashes
- CI run provenance links
- release ring recommendation

Runtime model entry point: `TheUnlocker.Modding.Runtime/Platform/ProductUpgradeServices.cs`.

API entry point: `GET /api/v1/build-farm/jobs`.

Marketplace entry point: `/builds`.

## Registry Federation Center

The federation surface makes multi-registry discovery inspectable:

- official, private, local, and community registry endpoints
- registry status, latency, and package counts
- per-registry trust policy text
- unsigned-package handling
- registry priority ordering
- merged federated search results
- allowed and blocked package decisions
- publisher trust levels
- policy version used for evaluation
- default trust threshold

Runtime model entry point: `TheUnlocker.Modding.Runtime/Platform/ProductUpgradeServices.cs`.

Runtime service entry point: `TheUnlocker.Modding.Runtime/Registry/FederatedRegistryService.cs`.

API entry point: `GET /api/v1/registries/federation`.

Marketplace entry point: `/federation`.

## In-App Documentation Hub

API entry point: `GET /api/v1/docs-hub`.

Marketplace entry point: `/docs-hub`.

Links should point to:

- SDK docs
- manifest schema
- packaging guide
- signing guide
- sample mods
- troubleshooting

## Governance Center

The governance surface groups administrator and operator state:

- moderation queue
- notification center
- effective enterprise/team policy
- detailed registry service health
- package risk flags
- policy sync status

API entry points:

- `GET /api/v1/admin/moderation`
- `GET /api/v1/notifications`
- `GET /api/v1/policy/effective`
- `GET /api/v1/policy/simulations`
- `GET /api/v1/registry/health`

Marketplace entry point: `/governance`.

## Enterprise Policy Simulation Lab

The policy simulation lab lets operators preview policy outcomes before enforcing them on every desktop client.

It models:

- trusted signature requirements
- registry allowlists
- permission denylists
- network permission consent
- stable, beta, and nightly update rings
- local developer review flows
- blocked unsafe package scenarios

The marketplace renders each scenario with:

- final decision
- package version
- registry source
- trust level
- requested permissions
- triggered findings
- recommended rollout actions

Runtime snapshot records live in `TheUnlocker.Modding.Runtime/Platform/ProductUpgradeServices.cs`.

API entry point: `GET /api/v1/policy/simulations`.

Marketplace entry point: `/policy-lab`.

## Desktop Release Center

The release center exposes desktop self-update state as a product surface:

- stable, beta, and nightly channels
- signed update requirement
- prerelease policy
- rollout percentages
- release health status
- rollback plan
- current desktop version
- last update check timestamp

Runtime entry point: `TheUnlocker.Modding.Runtime/Desktop/DesktopSelfUpdater.cs`.

API entry point: `GET /api/v1/releases/desktop`.

Marketplace entry point: `/releases`.

## Device Fleet And Remote Orchestration

The device fleet surface models signed-in desktop clients that can receive remote marketplace actions:

- online and offline client status
- active profile per device
- installed mod count per device
- registry URL per device
- LAN address for local discovery
- trusted-device state
- pending install and policy commands
- command readiness for push installs
- account-level fleet orchestration key

API entry point: `GET /api/v1/devices/fleet`.

Marketplace entry point: `/devices`.
