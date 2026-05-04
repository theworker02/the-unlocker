# Contributing

## Setup Matrix

| Tool | Version | Windows install | macOS/Linux install |
| --- | --- | --- | --- |
| .NET SDK | 8.x | `winget install Microsoft.DotNet.SDK.8` | `https://dotnet.microsoft.com/download` |
| Node.js | 22.x | `winget install OpenJS.NodeJS.LTS` | `nvm install 22` |
| Ruby | 3.3.x | `winget install RubyInstallerTeam.Ruby.3.3` | `rbenv install 3.3.0` |
| Go | 1.23.x | `winget install GoLang.Go` | `mise use -g go@1.23` |
| Rust | stable | `winget install Rustlang.Rustup` | `rustup default stable` |
| Docker Desktop | latest | `winget install Docker.DockerDesktop` | Docker package for your distro |
| just | latest | `winget install Casey.Just` | `cargo install just` |

## Devcontainer

The repository includes `.devcontainer/devcontainer.json` with .NET, Node, Ruby, Go, Rust, and Docker-in-Docker features.

Use it when you want reproducible local tooling without installing every language directly on Windows.

## Common Commands

```powershell
just build
just test
just frontend
just backend
just docker
```

Without `just`:

Recommended checks before opening a PR:

```text
dotnet build TheUnlockerWorkspace.slnx
dotnet test TheUnlocker.Tests
dotnet run --project TheUnlocker.ModPackager -- validate SampleMod
cd frontend/marketplace && npm ci && npm run build
cd backend/go-api && go test ./...
ruby -c backend/ruby-registry/app.rb
cargo check --manifest-path backend/rust-worker/Cargo.toml --locked
```

Keep new runtime features inside `TheUnlocker.Modding.Runtime` unless they are public SDK contracts, in which case they belong in `TheUnlocker.Modding.Abstractions`.
