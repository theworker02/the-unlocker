# CLI

The CLI project is `TheUnlocker.ModPackager`.

```text
dotnet run --project TheUnlocker.ModPackager -- init MyMod my-mod
dotnet run --project TheUnlocker.ModPackager -- validate MyMod
dotnet run --project TheUnlocker.ModPackager -- package MyMod packaged-mods
dotnet run --project TheUnlocker.ModPackager -- pack MyMod packaged-mods
dotnet run --project TheUnlocker.ModPackager -- install packaged-mods/my-mod-1.0.0.zip "%LOCALAPPDATA%/TheUnlocker/Mods"
dotnet run --project TheUnlocker.ModPackager -- resolve "%LOCALAPPDATA%/TheUnlocker/Mods"
dotnet run --project TheUnlocker.ModPackager -- keys keys local-dev
dotnet run --project TheUnlocker.ModPackager -- sign MyMod/bin/Debug/net8.0 keys/local-dev.private.pem
dotnet run --project TheUnlocker.ModPackager -- sign-package packaged-mods/my-mod-1.0.0.zip keys/local-dev.private.pem
dotnet run --project TheUnlocker.ModPackager -- verify-package packaged-mods/my-mod-1.0.0.zip packaged-mods/my-mod-1.0.0.zip.signature.json keys/local-dev.public.pem
dotnet run --project TheUnlocker.ModPackager -- publish packaged-mods/my-mod-1.0.0.zip registry.json https://example.com/my-mod.zip
dotnet run --project TheUnlocker.ModPackager -- generate-plan "Create a UI theme mod"
dotnet run --project TheUnlocker.ModPackager -- workspace MyModpack "Vanilla+" unity
dotnet run --project TheUnlocker.ModPackager -- lock MyModpack/mods MyModpack/unlocker.lock.json
dotnet run --project TheUnlocker.ModPackager -- export-modpack MyModpack VanillaPlus.zip
dotnet run --project TheUnlocker.ModPackager -- import-modpack VanillaPlus.zip ImportedPack
dotnet run --project TheUnlocker.ModPackager -- graph "%LOCALAPPDATA%/TheUnlocker/Mods" graph.mmd
dotnet run --project TheUnlocker.ModPackager -- protocol-reg TheUnlocker.exe theunlocker-protocol.reg
```

`package` includes `sbom.json` inside the generated mod archive.
