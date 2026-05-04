# Workspaces, Lockfiles, and Modpacks

The workspace format lets users share curated mod sets.

```text
MyModpack/
├── unlocker.json
├── unlocker.lock.json
└── mods/
```

## CLI

```powershell
dotnet run --project .\TheUnlocker.ModPackager -- workspace .\MyModpack "Vanilla+" unity
dotnet run --project .\TheUnlocker.ModPackager -- lock .\MyModpack\mods .\MyModpack\unlocker.lock.json
dotnet run --project .\TheUnlocker.ModPackager -- export-modpack .\MyModpack .\VanillaPlus.zip
dotnet run --project .\TheUnlocker.ModPackager -- import-modpack .\VanillaPlus.zip .\ImportedPack
```

`unlocker.lock.json` pins package hashes so a pack can be restored reproducibly.
