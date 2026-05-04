# ADR 0003: Multi-Artifact Release Pipeline

## Status

Accepted.

## Context

TheUnlocker ships more than one artifact: desktop app, mod SDK packages, templates, frontend container, Go API container, registry services, and worker services.

## Decision

GitHub Actions owns release packaging.

The release workflow builds:

- WPF desktop publish output
- Template NuGet package
- SDK/analyzer NuGet packages
- Frontend Docker image
- Go API Docker image
- Registry and worker Docker images

## Consequences

- Release output is reproducible.
- CI and release commands stay visible in the repository.
- Docker and NuGet publishing can be enabled later with repository secrets.
