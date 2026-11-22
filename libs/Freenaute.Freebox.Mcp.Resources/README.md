# Freenaute.Freebox.Mcp.Resources

`Freenaute.Freebox.Mcp.Resources` exposes documentation and live resources to MCP clients.

- Target: `net11.0`
- AOT posture: compatible (`EmbeddedResource` docs + typed resource handlers)
- Depends on: [Freenaute.Freebox.Mcp.Core](../Freenaute.Freebox.Mcp.Core/README.md)

## Resource inventory

### Documentation resources (static markdown)

- `freebox://docs/en/overview`
- `freebox://docs/en/resources`
- `freebox://docs/en/quickstart`
- `freebox://docs/en/auth-pairing`
- `freebox://docs/en/tool-families`
- `freebox://docs/fr/overview`
- `freebox://docs/fr/resources`
- `freebox://docs/fr/quickstart`
- `freebox://docs/fr/authentification-appairage`
- `freebox://docs/fr/tool-families`
- Domain catalogs: the 10 `freebox://docs/{en|fr}/domain-*` and `domaine-*` URIs

Source files are in:

- [Docs/en](./Docs/en)
- [Docs/fr](./Docs/fr)

### Live resource

- `freebox://live/status`
- `freebox://live/network-advanced`
- `freebox://live/files-advanced`
- `freebox://live/systemhome-controls`
- `freebox://live/services-controls`
- `freebox://live/parental-write`

Generated through:

- [OrchestratorFreeboxLiveResourceSource](./Live/OrchestratorFreeboxLiveResourceSource.cs)

## Design

- Static docs are embedded and served as markdown.
- Live status merges sanitized snapshots from Core orchestration.
- Resource reads are bounded and timeout-aware.

## Registration

Resource registration entry point:

- [ResourceServiceCollectionExtensions](./DependencyInjection/ResourceServiceCollectionExtensions.cs)

Production live source registration:

- `AddProductionFreeboxLiveResourceSource()`
