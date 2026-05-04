# The Unlocker WPF App

This document is for the desktop app project only. For the full repository overview, see the root [README.md](../README.md).

This WPF app includes a local-first modular content manager.

Runtime content lives under:

```text
%LOCALAPPDATA%\TheUnlocker\
```

The manager creates these on first launch:

```text
%LOCALAPPDATA%\TheUnlocker\Modules\
%LOCALAPPDATA%\TheUnlocker\Mods\
%LOCALAPPDATA%\TheUnlocker\Logs\
%LOCALAPPDATA%\TheUnlocker\content-config.json
```

## Enable a Module

Edit `content-config.json`:

```json
{
  "enabledModules": [
    "extra-textures"
  ]
}
```

Then add either a DLL:

```text
%LOCALAPPDATA%\TheUnlocker\Modules\extra-textures.dll
```

Or a manifest:

```text
%LOCALAPPDATA%\TheUnlocker\Modules\ExtraTextures\extra-textures.manifest.json
```

```json
{
  "id": "extra-textures",
  "name": "Extra Textures",
  "version": "1.0.0",
  "entryDll": "ExtraTextures.dll"
}
```

If the config enables a module but the manifest or DLL is missing, the UI shows it as `Missing`. If a manifest points to an entry DLL that is not present, the UI shows it as `Error`.

The main app checks feature gates through `IContentManager.IsModuleEnabled("module-id")`, so optional content can fail closed without crashing startup.

## Load a Code Mod

Code mods live under:

```text
%LOCALAPPDATA%\TheUnlocker\Mods\
```

A mod package is a folder containing `mod.json` beside the entry DLL:

```text
%LOCALAPPDATA%\TheUnlocker\Mods\hello-world\mod.json
%LOCALAPPDATA%\TheUnlocker\Mods\hello-world\SampleMod.dll
```

```json
{
  "id": "hello-world",
  "name": "Hello World Mod",
  "version": "1.0.0",
  "author": "The Unlocker",
  "description": "A sample mod.",
  "entryDll": "SampleMod.dll",
  "minimumAppVersion": "1.0.0",
  "minimumFrameworkVersion": "8.0.0",
  "dependsOn": [],
  "permissions": ["ReadAssets", "AddMenuItems", "SendNotifications", "Settings"],
  "settings": {
    "greeting": {
      "label": "Greeting",
      "type": "text",
      "defaultValue": "Hello from the sample mod."
    }
  },
  "publisherId": "local-samples",
  "signature": {
    "sha256": "OPTIONAL_ENTRY_DLL_SHA256"
  }
}
```

Enable code mods in `content-config.json`, or use the checkbox in the app:

```json
{
  "enabledModules": [],
  "enabledMods": ["hello-world"],
  "trustedPublishers": ["local-samples"],
  "modSettings": {
    "hello-world": {
      "greeting": "Hello from config."
    }
  }
}
```

The loader supports:

- `mod.json` manifests with author, description, dependencies, permissions, settings, and compatibility fields.
- Enable/disable state for code mods.
- Dependency ordering with missing/disabled dependency errors.
- App and runtime version compatibility checks.
- Per-mod collectible `AssemblyLoadContext` isolation for dependency conflicts and hot reload.
- Full log output at `%LOCALAPPDATA%\TheUnlocker\Logs\mod-loader.log`.
- Import of `.zip` and `.dll` packages from the UI.
- Permission-gated services exposed through `IModContext`.
- Optional SHA-256 signature checking plus trusted publisher warnings.
- Crash protection around `OnLoad` and `OnUnload`.

There is also a compile-tested `SampleMod` project in the repository. Mod authors should reference `TheUnlocker.Modding.Abstractions` instead of the WPF host app.

Package the sample mod:

```text
dotnet run --project TheUnlocker.ModPackager -- package SampleMod packaged-mods
```

The packager builds the mod, validates/copies `mod.json`, hashes the entry DLL, writes the hash into the package manifest, and creates a `.zip`.

Additional manager features:

- `dotnet new` template source at `templates/theunlocker-mod`.
- Author CLI commands: `init`, `validate`, `package`, `keys`, `sign`, and `publish`.
- Local package registry with package cache and install history.
- Trust levels: Official, Trusted Publisher, Local Developer, Unknown, Blocked.
- Dependency version ranges and optional dependencies.
- Load phases and async mod lifecycle interfaces.
- Out-of-process isolation mode is represented in manifests and staged as a non-in-process load mode.
- Event schema registration.
- Command scopes through command palette registration overloads.
- Runtime telemetry includes event handlers, commands, exceptions, average load time, and last successful load metadata.
- In-app structured log viewer.
- Safe mode through `safeMode` in `content-config.json`.
- Staging install area before promotion to active mods.
- Update flow includes changelog and permission diff fields.
- Mod documentation generator from manifest metadata.
- Runtime code is split into `TheUnlocker.Modding.Runtime`; the WPF app is now a shell over the runtime.
- Mod authors reference only `TheUnlocker.Modding.Abstractions`.
- `sdkVersion` compatibility checks reject incompatible major SDK versions.
- Capability tokens back permission-gated services instead of raw string checks.
- Private mod dependencies can live under `Mods\your-mod\lib\`.
- Unload verification logs collectible `AssemblyLoadContext` release counts.
- `Mods` and `Modules` are watched with debounced automatic refresh.
- Profiles through `profiles` and `activeProfile` in `content-config.json`, plus the editable profile selector in the UI.
- Load order, conflict, update, and health tabs.
- Conflict warnings via the manifest `targets` field.
- Marketplace entries are shown from `repositoryIndexPath`; selected entries can be installed through the UI.
- Secure marketplace installs verify the repository entry `sha256` before atomic install.
- Public-key signatures are supported with `signature.rsaSha256` and `trustedPublisherKeys`.
- Enterprise policy supports `allowUnsignedMods`, `allowedPublishers`, and `blockedMods`.
- Mods can implement `IModMigration` for settings migrations.
- Repeated crashes disable a mod and mark it unsafe.
- Repository update detection through `repositoryIndexPath`, which can point to a local JSON file or HTTP/HTTPS JSON feed.
- Import rollback and quarantine at `%LOCALAPPDATA%\TheUnlocker\Quarantine`.
- Permission consent prompt before enabling a mod with declared permissions.
- Event bus through `context.Events.Subscribe<T>()` and `context.Events.Publish<T>()`.
- Typed settings for `text`, `boolean`, `number`, and `select`.
- Diagnostics export writes a zip containing config, logs, health, manifests, and recent errors.
- Sample extension points include navigation items, asset importers, themes, command palette commands, and tool panels.

Repository index example:

```json
{
  "mods": [
    {
      "id": "hello-world",
      "name": "Hello World Mod",
      "version": "1.1.0",
      "description": "Sample marketplace listing.",
      "downloadUrl": "https://example.com/mods/hello-world-1.1.0.zip",
      "sha256": "OPTIONAL_PACKAGE_SHA256"
    }
  ]
}
```

Run the xUnit test suite:

```text
dotnet test TheUnlocker.Tests
```

Author CLI examples:

```text
dotnet run --project TheUnlocker.ModPackager -- init MyNewMod my-new-mod
dotnet run --project TheUnlocker.ModPackager -- validate SampleMod
dotnet run --project TheUnlocker.ModPackager -- package SampleMod packaged-mods
dotnet run --project TheUnlocker.ModPackager -- keys keys local-samples
dotnet run --project TheUnlocker.ModPackager -- sign SampleMod\bin\Debug\net8.0-windows keys\local-samples.private.pem
dotnet run --project TheUnlocker.ModPackager -- publish packaged-mods\hello-world-1.0.0.zip SampleMod\repository-index.sample.json https://example.com/mods/hello-world-1.0.0.zip
```

Template install example:

```text
dotnet new install templates\theunlocker-mod
dotnet new theunlocker-mod -n MyMod
```

The loader intentionally supports sanctioned `IMod` extension points only. It does not patch licensing, ownership, integrity, authentication, or other protected application logic.
