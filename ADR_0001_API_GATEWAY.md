# ADR 0001: Go As The Public API Gateway

## Status

Accepted.

## Context

TheUnlocker has multiple backend services: Ruby, Go, Rust, and .NET. Browser, desktop, CLI, and automation clients need a stable public API that does not change every time an internal service changes.

## Decision

Go owns the public versioned API namespace:

```text
/api/v1/...
```

Ruby and .NET remain internal service implementations behind the Go gateway.

## Consequences

- Frontend clients call the Go gateway.
- Desktop sync and install clients call the Go gateway.
- Ruby continues to own accounts and sessions until that data model moves.
- .NET continues to own registry metadata and compatibility workflows where already implemented.
- Go can read Mongo directly for public routes when ownership allows it.
