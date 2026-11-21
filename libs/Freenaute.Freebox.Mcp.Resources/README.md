# Freenaute.Freebox.Mcp.Resources

MCP **resources** exposed by the server: embedded reference documentation (`freebox://docs/...`) and the live
sanitized status snapshot (`freebox://live/status`). Targets `net11.0` / C# 15, `IsAotCompatible` + `IsTrimmable`,
uses the .NET MCP SDK.

## Resources (`[McpServerResourceType]`)

| URI                           | Name                        | Content                                                                             |
|-------------------------------|-----------------------------|-------------------------------------------------------------------------------------|
| `freebox://docs/en/overview`  | `freebox-docs-en-overview`  | English overview (capabilities, pairing workflow, safety rules) — LLM-facing.       |
| `freebox://docs/en/resources` | `freebox-docs-en-resources` | English catalog of tools/resources with examples — LLM-facing.                      |
| `freebox://docs/fr/overview`  | `freebox-docs-fr-overview`  | French overview.                                                                    |
| `freebox://docs/fr/resources` | `freebox-docs-fr-resources` | French catalog.                                                                     |
| `freebox://live/status`       | `freebox-live-status`       | Current sanitized Freebox status as markdown (pairing / network / system sections). |

All are served as `text/markdown`.

## How the docs are embedded

`Docs/**/*.md` are declared as `<EmbeddedResource>` in the `.csproj` and loaded by
`Documentation/DocumentationStore.cs` from the assembly manifest resources
(`Freenaute.Freebox.Mcp.Resources.Docs.en.overview.md`, ...). Lookups are **case-insensitive**; each document is
bounded to **64 KiB** (truncated with a marker if larger — enforced by `DocumentationTests`). The markdown files are
written for an LLM consumer: they describe capabilities, the pairing workflow, safety rules, limits, and example
queries, because they are injected into the model context when the client reads the resource.

## Live resource

`Live/LiveResources.cs` reads `freebox://live/status` through the `IFreeboxLiveResourceSource` abstraction (10 s
timeout, cancellation propagated, output truncated at 64 KiB, media type sanitized). The production
implementation `OrchestratorFreeboxLiveResourceSource` aggregates the orchestrator's connection, network and system
snapshots into a markdown report with bounded counts and sanitized tokens (alphanumerics + `_-.`, max 32 chars).
Register it with `AddProductionFreeboxLiveResourceSource()` or substitute your own via
`AddFreeboxLiveResourceSource(...)`
— see `DependencyInjection/ResourceServiceCollectionExtensions.cs` (`AddFreeboxResources()` registers both resource
types with the MCP server builder).

## Dependencies

- Project: `Freenaute.Freebox.Mcp.Core` (orchestrator abstractions for the live source).
- NuGet: `ModelContextProtocol` (CPM).
