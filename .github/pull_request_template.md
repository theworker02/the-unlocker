## Summary

- 

## Verification

- [ ] `dotnet build .\TheUnlockerWorkspace.slnx`
- [ ] `dotnet test .\TheUnlocker.Tests\TheUnlocker.Tests.csproj`
- [ ] `npm run build` in `frontend/marketplace`
- [ ] `go test ./...` in `backend/go-api`
- [ ] `cargo check --manifest-path .\backend\rust-worker\Cargo.toml`
- [ ] `ruby -c .\backend\ruby-registry\app.rb`

## Safety Boundary

- [ ] This change does not add licensing bypasses, anti-cheat bypasses, authentication bypasses, integrity-check evasion, or arbitrary protected runtime patching.
