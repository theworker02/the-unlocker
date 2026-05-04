# Registry

The registry index can be local JSON or served by the hosted registry REST API.

```json
{
  "mods": [
    {
      "id": "better-graphics",
      "name": "Better Graphics",
      "version": "1.2.0",
      "description": "Improves visual quality.",
      "downloadUrl": "https://example.com/mods/better-graphics-1.2.0.zip",
      "sha256": "PACKAGE_SHA256",
      "permissions": ["ReadAssets"],
      "changelog": "Improved lighting presets."
    }
  ]
}
```

Hosted REST API:

```text
GET  /mods
GET  /mods/{id}
POST /mods
POST /mods/{id}/versions
POST /mods/{id}/review
POST /mods/{id}/flags
POST /mods/{id}/ratings
POST /mods/{id}/comments
POST /crash-reports
POST /sync/{userId}
```

`RegistryClient.ApiClient` provides a small client for fetching indexes and publishing entries.
