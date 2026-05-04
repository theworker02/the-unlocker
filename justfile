set shell := ["powershell.exe", "-NoLogo", "-Command"]

build:
    dotnet build .\TheUnlockerWorkspace.slnx

test:
    dotnet test .\TheUnlocker.Tests\TheUnlocker.Tests.csproj

frontend:
    cd frontend/marketplace; npm ci; npm run build

backend:
    ruby -c .\backend\ruby-registry\app.rb
    cd backend/go-api; go test ./...
    cargo check --manifest-path .\backend\rust-worker\Cargo.toml --locked

docker:
    docker compose -f .\deploy\docker-compose.yml up --build

pack-template:
    dotnet pack .\TheUnlocker.Templates.csproj

package-sample:
    dotnet run --project .\TheUnlocker.ModPackager -- package .\SampleMod .\packaged-mods
