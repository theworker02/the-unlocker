# ADR 0002: Explicit Database Ownership

## Status

Accepted.

## Context

Multiple services can access MongoDB, Redis, and object storage. Without ownership rules, the same data can drift across Go, Ruby, Rust, and .NET implementations.

## Decision

Each collection has a documented owner in [DATABASE_OWNERSHIP.md](DATABASE_OWNERSHIP.md).

Services may read another service's data only when documented.

Mutation should happen through the owning service or through a clearly documented transition plan.

## Consequences

- Go can expose public routes while proxying to Ruby or .NET.
- Workers consume jobs but do not silently own unrelated collections.
- Schema changes require documentation updates.
