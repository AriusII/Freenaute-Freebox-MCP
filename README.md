# Freenaute Freebox MCP

[![NuGet](https://img.shields.io/nuget/v/Freenaute.Freebox.Mcp)](https://www.nuget.org/packages/Freenaute.Freebox.Mcp)
[![.NET](https://img.shields.io/badge/.NET-11.0-512BD4)](https://dotnet.microsoft.com/)
[![License: MIT](#license)

A **local, stdio-only Model Context Protocol (MCP) server** that exposes a Freebox Server (Freebox OS API) to LLM
clients: discovery, pairing coordination, and **read-only** connection / LAN / Wi-Fi / system status.

- **Transport:** stdio only (JSON-RPC on stdin/stdout). All logs and diagnostics go to **stderr**, so stdout stays a
  clean MCP channel.
- **Packaging:** NuGet tool (`DotnetTool` + `McpServer` package types) with a `.mcp/server.json` manifest. Run it with
  `dnx`, as an installed .NET tool, or as a self-contained **Native AOT single-file** binary.
- **Runtime:** C# 15 / .NET 11 (`net11.0`), `IsAotCompatible`, invariant globalization, source-generated JSON
  serialization everywhere.
- **Freebox client:** NuGet package [`Freenaute.Freebox.Client`](https://www.nuget.org/packages/Freenaute.Freebox.Client)
  `1.0.0` implements the Freebox OS HTTP API and the pairing protocol.
- **Safety first:** every tool result is sanitized and bounded (no hostnames, IP/MAC addresses, SSIDs, tokens);
  pairing always requires **physical approval on the Freebox display**; the durable app token is stored **only** in
  the OS credential vault, never in plaintext.

Useful links:

- [Freebox OS API documentation](https://dev.freebox.fr/sdk/os/)
- [Model Context Protocol](https://modelcontextprotocol.io/) — [specification](https://spec.modelcontextprotocol.io/) —
  [.NET MCP SDK](https://csharp.sdk.modelcontextprotocol.io/) — [NuGet MCP server guide](https://aka.ms/nuget/mcp/guide)

## What it can do

### MCP tools (8)

| Tool | Read-only | Purpose |
| --- | --- | --- |
| `freebox_discover` | yes | Discover the reachable Freebox; returns bounded, non-identifying API metadata (model, API version, HTTPS port). |
| `freebox_pairing_status` | yes | Report durable credential state (`Missing`, `Pending`, `Approved`, `Denied`, `Expired`, `Corrupt`, `Unavailable`) without exposing secrets. |
| `freebox_pairing_initiate` | no (gated) | Start a new pairing. Only allowed when `FREEBOX_ALLOW_PAIRING_INITIATION` is `1`/`true`/`TRUE`. Physical approval is still required. |
| `freebox_pairing_wait` | no | Poll an existing pairing until physical approval; cancellable/resumable with optional poll interval and timeout overrides. |
| `freebox_connection_status` | yes | Connection state, type, media, rates, traffic counters. Network addresses are excluded. |
| `freebox_lan_overview` | yes | Bounded LAN summary per interface; optional hosts (`includeHosts`, `interfaceName`). Host identifiers are SHA-256 hashes; names/domains/addresses are excluded. |
| `freebox_wifi_status` | yes | Sanitized Wi-Fi state, access points and BSS list. SSIDs, BSSIDs and host identifiers are excluded/hashed. |
| `freebox_system_status` | yes | Firmware, model, uptime, disk, sensors, fans, expansion slots (bounded, sanitized). |

### MCP resources

- `freebox://docs/en/overview`, `freebox://docs/en/resources`, `freebox://docs/en/quickstart`, `freebox://docs/en/auth-pairing`
- `freebox://docs/fr/overview`, `freebox://docs/fr/resources`, `freebox://docs/fr/quickstart`, `freebox://docs/fr/authentification-appairage`
- `freebox://live/status` — current sanitized status snapshot (markdown, bounded to 64 KiB)

The `freebox://docs/...` documents are embedded resources (`EmbeddedResource` in
`libs/Freenaute.Freebox.Mcp.Resources/Docs/**`) served as `text/markdown`; they double as LLM context.

## Repository layout

```text
.
├── mcp/Freenaute.Freebox.Mcp/        # MCP host: Program.cs, Security/ (credential storage), .mcp/server.json, README (NuGet package readme)
├── libs/
│   ├── Freenaute.Freebox.Mcp.Core/   # Options, DI composition, SDK repositories/services, orchestrator, error mapping, credential store, redaction
│   ├── Freenaute.Freebox.Mcp.Tools/  # The 8 [McpServerTool] classes + sanitization (Safe) + result DTOs
│   └── Freenaute.Freebox.Mcp.Resources/ # MCP resources: embedded Docs/** and freebox://live/status
├── tests/                            # xUnit v3 + Microsoft.Testing.Platform (Core, Tools, Resources, Protocol/stdio)
├── containers/                       # Linux multi-arch OCI image (Dockerfile + README)
├── .github/workflows/                # CI (build/test/pack) and release (AOT matrix, NuGet, OCI, SBOM)
├── .artifacts/                       # ALL build outputs (UseArtifactsOutput): publish/, package/, release/
├── Directory.Build.props             # LangVersion 15, Nullable, TreatWarningsAsErrors, AOT/trimming analyzers, lock files, artifacts path
├── Directory.Build.targets           # Root-owned MSBuild orchestration targets
├── Directory.Packages.props          # Central Package Management (CPM)
├── global.json                       # .NET SDK 11.0.100 (rollForward: disable), MTP test runner
└── Freenaute.Freebox.MCP.slnx        # Solution (XML format)
```

## Quickstart

Requires the **.NET 11 SDK** (pinned to `11.0.100` in `global.json`).

### 1. Run from source (stdio)

```powershell
dotnet run --project mcp/Freenaute.Freebox.Mcp/Freenaute.Freebox.Mcp.csproj
```

Register it in your MCP client (e.g. `mcpServers` in VS Code / Claude Desktop config):

```json
{
  "mcpServers": {
    "freenaute-freebox-mcp": {
      "type": "stdio",
      "command": "dotnet",
      "args": ["run", "--project", "mcp/Freenaute.Freebox.Mcp/Freenaute.Freebox.Mcp.csproj"]
    }
  }
}
```

### 2. Run from NuGet (dnx)

```powershell
dnx Freenaute.Freebox.Mcp --version 0.1.0-beta --yes
```

```json
{
  "mcpServers": {
    "freenaute-freebox-mcp": {
      "type": "stdio",
      "command": "dnx",
      "args": ["Freenaute.Freebox.Mcp", "--version", "0.1.0-beta", "--yes"]
    }
  }
}
```

The package identity in `.mcp/server.json` is `org.freenaute/freenaute-freebox-mcp`.

### 3. Install as a .NET global tool

```powershell
dotnet tool install --global Freenaute.Freebox.Mcp
freenaute-freebox-mcp          # stdio server
```

### 4. Run the Native AOT binary

```powershell
dotnet publish mcp/Freenaute.Freebox.Mcp/Freenaute.Freebox.Mcp.csproj `
  --configuration Release --runtime win-x64 --self-contained true `
  -p:PublishAot=true --output .artifacts/publish/Freenaute.Freebox.Mcp/release_win-x64
# then launch the single-file executable directly; no runtime install needed
```

`PublishAot` and `InvariantGlobalization` are project-owned settings; the explicit
`-p:PublishAot=true` above makes the release intent visible. Publishing is RID-specific, so
smoke-test the binary for each target. A Linux AOT binary runs only on the same or newer
compatible Linux version on which it was built.

Supported publish RIDs: `win-x64`, `win-arm64`, `linux-x64`, `linux-arm64` (Windows + Linux, x64 + ARM only).
The repository enables `IsAotCompatible` plus trimming and single-file analysis and treats
warnings as errors. Review and resolve publish warnings rather than suppressing them; AOT
trimming and Native AOT do not support unbounded runtime reflection or runtime code generation.
See [Microsoft Learn: Native AOT deployment](https://learn.microsoft.com/dotnet/core/deploying/native-aot/).

### 5. Run in a container (Linux amd64/arm64)

See [containers/README.md](containers/README.md). Because the transport is stdio, run attached with `-i` (never `-t`):

```powershell
docker run --rm -i ghcr.io/freenaute/freenaute-freebox-mcp:0.1.0-beta
```

## Configuration (environment variables)

All optional; all are declared in `mcp/Freenaute.Freebox.Mcp/.mcp/server.json`.

| Variable | Default | Meaning |
| --- | --- | --- |
| `FREEBOX_APPLICATION_ID` | `org.freenaute.freebox-mcp` | Freebox application identifier registered during pairing. |
| `FREEBOX_APPLICATION_NAME` | `Freenaute Freebox MCP` | Display name shown on the Freebox during pairing. |
| `FREEBOX_DEVICE_NAME` | machine name (`Environment.MachineName`) | Client device name shown during pairing. |
| `FREEBOX_CREDENTIAL_PROFILE` | `default` | Local credential profile name; scopes the stored credential together with the application ID and the Freebox UID. |
| `FREEBOX_ALLOW_PAIRING_INITIATION` | unset (disabled) | Opt-in switch: pairing initiation is allowed **only** when set exactly to `1`, `true`, or `TRUE`. Waiting on an already-started pairing (`freebox_pairing_wait`) does not require it. |
| `FREEBOX_SERVER_ADDRESS` | `https://mafreebox.freebox.fr/` | Absolute http(s) origin of the Freebox API (no path/credentials/query). |
| `FREEBOX_PAIRING_POLL_INTERVAL_SECONDS` | `2` | Default polling interval for `freebox_pairing_wait` when no tool override is provided; must be `> 0`. |
| `FREEBOX_PAIRING_WAIT_TIMEOUT_SECONDS` | `600` | Default timeout for `freebox_pairing_wait` when no tool override is provided; must be `> 0`. |
| `FREEBOX_AUTHENTICATE_ON_STARTUP` | `true` | Enables startup authentication bootstrap: reuse persisted credential state and, if policy allows, startup pairing wait workflow. |
| `FREEBOX_CREDENTIAL_FILE` | unset | Optional encrypted-file store (AES-256-GCM) for containers/headless; enables storage only when set. |
| `FREEBOX_CREDENTIAL_PASSPHRASE_FILE` | unset | File containing the passphrase for `FREEBOX_CREDENTIAL_FILE` (mounted secret, e.g. `/run/secrets/...`). |

The Freebox server address defaults to `https://mafreebox.freebox.fr/` (see `FREEBOX_SERVER_ADDRESS` and `FreeboxCoreOptions.ServerAddress`).

**Never** put a token, app secret, or credential in an environment variable, argv, an MCP config file, or a log.

## Pairing flow (step by step)

The HTTP flow below is implemented by `Freenaute.Freebox.Client`; the MCP server orchestrates it:

1. **Initiate** (gated): the client sends `POST /api/v*/login/authorize/` with the application id/name/device name and
   permissions, and receives an `app_token` + `track_id`.
2. **Physical approval (mandatory):** the user must approve the application **on the Freebox display (LCD)**. There
   is no way to bypass this; `freebox_pairing_initiate` refuses to run at all unless
   `FREEBOX_ALLOW_PAIRING_INITIATION` opts in.
3. **Poll**: `GET /api/v*/login/authorize/{track_id}` returns `pending`, `granted`, `denied`, or `timeout`.
   `freebox_pairing_wait` polls this and can be cancelled/resumed safely, with optional per-call polling/timeout overrides.
4. **Session**: on `granted`, `GET /api/v*/login/` returns a `challenge`; the client computes
   `password = HMAC-SHA1(app_token, challenge)` and posts it to `POST /api/v*/login/session/`, receiving a
   `session_token`.
5. **Use**: the `session_token` is kept **in memory only** and sent as the `X-Fbx-App-Auth` header on every API call.
   It is never persisted.
6. **Persist**: only the durable `app_token` (plus track id, status, timestamps) is stored, **encrypted by the OS
   credential vault** (see below), scoped by SHA-256(Freebox UID + application ID + profile).

Typical MCP sequence for a client:

```text
freebox_discover            -> is a Freebox reachable?
freebox_pairing_status      -> credential state (Missing on first run)
freebox_pairing_initiate    -> only if FREEBOX_ALLOW_PAIRING_INITIATION=1 ; then approve on the Freebox LCD
freebox_pairing_wait        -> blocks until granted/denied/timeout (cancellable)
freebox_connection_status   -> online?
freebox_lan_overview        -> LAN summary
freebox_wifi_status         -> Wi-Fi summary
freebox_system_status       -> firmware/uptime/sensors/fans
```

### Command-line policy

The MCP server accepts **no launch arguments**. Pairing/bootstrap is performed through MCP tools and startup
authentication only.

## Security model

Details: [mcp/Freenaute.Freebox.Mcp/Security/README.md](mcp/Freenaute.Freebox.Mcp/Security/README.md).

- **Credential storage matrix**
  - Windows: Credential Manager via `CredWriteW`/`CredReadW` (`CRED_TYPE_GENERIC`, `CRED_PERSIST_LOCAL_MACHINE`),
    DPAPI-encrypted, local to the logon environment.
  - Linux: **libsecret / Secret Service** via the `secret-tool` CLI when available.
  - Containers / headless: optional **AES-256-GCM encrypted file** (`FREEBOX_CREDENTIAL_FILE` +
    `FREEBOX_CREDENTIAL_PASSPHRASE_FILE`, typically mounted under `/run/secrets/`).
  - Anything else: **fail-closed** (`UnsupportedCredentialStorage`) — nothing written to plaintext files,
    re-pairing required on restart. There is deliberately never a plaintext fallback.
  - Selection order: encrypted-file env opt-in → Windows Credential Manager → Linux libsecret → fail-closed.
- **Key scoping:** storage key = `org.freenaute.freebox-mcp/credentials:` + hex(SHA-256(length-prefixed
  Freebox UID + application ID + profile)). Buffers are zeroized after use.
- **Payload:** versioned JSON (format version 1) serialized with a source-generated `JsonSerializerContext`;
  every write is followed by a read-back verification with a fixed-time comparison.
- **Session token:** memory only, header `X-Fbx-App-Auth`.
- **Log redaction:** `app_token`, `track_id`, `session_token`, `challenge`, `password`, `authorization`, `cookie`
  values are redacted from any logged text (`FreeboxSecretRedactor`). Logs go to stderr only.
- **Output sanitization:** tool results are bounded (string length caps, collection caps: 32 interfaces / 128 hosts
  per interface / 32 APs / 128 BSS / 32 components) and stripped of control characters; host, sensor and BSS
  identifiers are replaced by SHA-256 hex digests; hostnames, domains, IP/MAC addresses, SSIDs and BSSIDs are never
  returned.
- **Error mapping:** failures are mapped to stable error codes (`AuthenticationRequired`, `AuthorizationPending`,
  `AuthorizationDenied`, `AuthorizationExpired`, `ApiRejected`, `TransportFailure`, `CredentialStoreUnavailable`, ...)
  with redacted messages.

## Building

Everything is standard MSBuild / dotnet CLI (no helper scripts). All outputs land in `.artifacts/` (`UseArtifactsOutput`).

```powershell
# Restore + build the whole solution
dotnet restore Freenaute.Freebox.MCP.slnx
dotnet build Freenaute.Freebox.MCP.slnx --configuration Release --no-restore

# Root-owned MSBuild orchestration targets (Directory.Build.targets)
dotnet msbuild -t:ValidateMetadata        # package/tool metadata consistency
dotnet msbuild -t:ValidateContainers      # container image policy (no Docker required)
dotnet msbuild -t:PublishAll              # Native AOT publish for every RID
dotnet msbuild -t:Pack                    # NuGet packages -> .artifacts/package
dotnet msbuild -t:Archive                 # release archives -> .artifacts/release
dotnet msbuild -t:PublishContainers       # OCI images (linux-x64/linux-arm64)
dotnet msbuild -t:BuildMcpHost            # managed protocol host for tests

# Direct equivalents
dotnet publish mcp/Freenaute.Freebox.Mcp/Freenaute.Freebox.Mcp.csproj -c Release -r linux-x64 -p:PublishAot=true
dotnet pack mcp/Freenaute.Freebox.Mcp/Freenaute.Freebox.Mcp.csproj -c Release   # -> .artifacts/package
```

NuGet packages are managed centrally (`Directory.Packages.props`, CPM) with lock files
(`RestorePackagesWithLockFile`); the only package sources is nuget.org (`NuGet.Config`).

### Startup composition

`Program.cs` is the composition root. It completes all `IServiceCollection` registrations
before `builder.Build()` and starts the host once with `RunAsync()`. Reusable
`AddFreebox...` extensions keep feature-owned lifetimes in their own assemblies; application
code consumes interfaces rather than a separate provider or service-locator root.
`ValidateOnStart()` validates options during host startup, while the container owns
construction and disposal of services it creates. New registrations should follow that
pattern and enable scope validation in tests rather than calling `BuildServiceProvider()`
during composition. See Microsoft Learn:
[DI overview](https://learn.microsoft.com/dotnet/core/extensions/dependency-injection/overview)
and [DI guidelines](https://learn.microsoft.com/dotnet/core/extensions/dependency-injection/guidelines).

## Tests

xUnit v3 with the Microsoft.Testing.Platform runner (see `global.json` `test.runner`):

```powershell
dotnet test Freenaute.Freebox.MCP.slnx --configuration Release
```

Test projects: `Freenaute.Freebox.Mcp.Core.Tests`, `...Tools.Tests`, `...Resources.Tests`,
`...Protocol.Tests` (the latter spawns the real stdio host and asserts `initialize`, `tools/list`,
`resources/list`, and `resources/read` behavior).

## Containers

Linux multi-arch OCI images (amd64 + arm64), chiseled `runtime-deps` base, non-root **UID 1654**, Native AOT
single-file, no shell — see [containers/README.md](containers/README.md). No macOS or Windows container images are
produced. Inside the Linux image the credential backend fails closed by design, so pairing must happen on a host
with an OS vault (Windows/macOS) beforehand.

## License

[MIT](LICENSE) — see `PackageLicenseExpression` in `Directory.Build.props`.
