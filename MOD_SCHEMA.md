# Mod Schema

```json
{
  "id": "better-graphics",
  "name": "Better Graphics",
  "version": "1.2.0",
  "author": "Matt",
  "description": "Improves visuals.",
  "entryDll": "BetterGraphics.dll",
  "minimumAppVersion": "1.0.0",
  "minimumFrameworkVersion": "8.0.0",
  "sdkVersion": "1.0.0",
  "dependsOn": [],
  "dependencies": [
    {
      "id": "shared-ui",
      "versionRange": ">=1.2.0 <2.0.0",
      "optional": false
    }
  ],
  "permissions": ["ReadAssets"],
  "targets": ["UI", "Rendering"],
  "eventSchemas": ["graphics.profile.changed"],
  "commandScopes": {
    "Open Graphics Panel": ["local"]
  },
  "settings": {},
  "publisherId": "local-dev",
  "trustLevel": "LocalDeveloper",
  "isolationMode": "InProcess"
}
```
