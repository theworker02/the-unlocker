# Security Model

The Unlocker security model is based on explicit trust and declared capabilities.

## Supported Controls

- SHA-256 package verification.
- RSA manifest signature verification.
- Trusted publisher keys.
- Policy rules:
  - `allowUnsignedMods`
  - `allowedPublishers`
  - `blockedMods`
- Permission consent before enabling mods.
- Capability-token-backed services.
- Staging before active install.
- Quarantine on failed import.
- Safe mode.
- Crash disablement.
- bcrypt password hashing for registry accounts.
- Short-lived bearer sessions plus refresh tokens.
- Session revocation.
- Login audit records.
- Go API auth middleware for protected client routes.
- Go API bearer token shape validation and scoped API key prefix validation.
- Baseline browser response hardening headers from the Go gateway.
- CORS allowlisting for local frontend origins instead of wildcard origins.
- `Cache-Control: no-store` for JSON API responses.
- Enterprise policy simulation before rollout.
- Permission diffs before updates.
- Signature enforcement policy decisions.
- SBOM generation for package contents.
- Quarantine review models for unsafe packages.

## Account And Session Security

- Registry passwords are hashed with bcrypt in the Ruby registry facade.
- Sessions include short-lived bearer tokens plus refresh tokens.
- Logout and explicit revoke flows mark sessions as revoked instead of silently deleting history.
- Login and registration attempts are written to a `login_audit` collection.
- The Go API gateway protects `/api/v1/me`, account settings, installs, modpacks, reports, jobs, and crash report submission through reusable auth middleware.
- Protected Go routes reject malformed `Authorization` values. Bearer credentials must use the `Bearer <token>` shape and scoped API keys must use the TheUnlocker `tu_` prefix.
- Gateway responses include `Content-Security-Policy`, `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, `Cross-Origin-Resource-Policy`, `X-Permitted-Cross-Domain-Policies`, and `Cache-Control`.

## Policy Simulation

The policy simulation endpoint previews decisions before a policy is rolled out. It reports allow, review, and block outcomes for package signatures, registry allowlists, permission denylists, network consent, and update rings.

## Update Permission Review

The runtime includes a permission diff service that compares an installed manifest against an update manifest. If the update adds permissions, the update requires re-approval before it should be enabled.

## Signature Enforcement

The runtime includes a signature policy evaluator for rules such as:

```json
{
  "allowUnsignedMods": false,
  "allowedPublishers": ["studio-official"],
  "blockedMods": ["bad-mod"]
}
```

## Package SBOMs

The registry runtime includes an SBOM generator for package DLLs, executables, and manifest files. These SBOM documents are intended to be stored beside provenance attestations and scan results.

## Quarantine Review

The desktop runtime includes a quarantine review model so invalid, broken, or unsafe packages can be inspected before restore or deletion.

## Not Included

The platform intentionally does not implement arbitrary process injection, DRM bypassing, anti-cheat bypassing, or integrity-check evasion.
