<!-- mcp-name: org.freenaute/freenaute-freebox-mcp -->

# Freenaute Freebox MCP

Local **stdio** Model Context Protocol server for Freebox discovery, pairing coordination, and read-only
system/network status. Pairing always requires explicit **physical approval on the Freebox display**.

- Package: `Freenaute.Freebox.Mcp` (NuGet, package types `DotnetTool` + `McpServer`)
- MCP server name (`.mcp/server.json`): `org.freenaute/freenaute-freebox-mcp`
- Tool command: `freenaute-freebox-mcp`
- Runtime: .NET 11 (`net11.0`), C# 15, Native AOT, self-contained, single-file, invariant globalization
- Transport: **stdio only** — JSON-RPC on stdin/stdout, all logs on stderr
- Launch policy: **no command-line arguments** are accepted

## Install

```powershell
# .NET global tool
dotnet tool install --global Freenaute.Freebox.Mcp

# or run without installing (dnx)
dnx Freenaute.Freebox.Mcp --version 1.0.0 --yes
```

## Use from an MCP client

```json
{
  "mcpServers": {
    "freenaute-freebox-mcp": {
      "type": "stdio",
      "command": "freenaute-freebox-mcp"
    }
  }
}
```

## Configuration (environment variables)

All optional; configured by host runtime environment:

| Variable                                | Default                         | Meaning                                                                                                                                                                            |
|-----------------------------------------|---------------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `FREEBOX_APPLICATION_ID`                | `org.freenaute.freebox-mcp`     | Registered Freebox application identifier.                                                                                                                                         |
| `FREEBOX_APPLICATION_NAME`              | `Freenaute Freebox MCP`         | Application name shown during pairing.                                                                                                                                             |
| `FREEBOX_DEVICE_NAME`                   | machine name                    | Device name shown during pairing.                                                                                                                                                  |
| `FREEBOX_CREDENTIAL_PROFILE`            | `default`                       | Credential profile; scopes the stored credential with the application ID and Freebox UID.                                                                                          |
| `FREEBOX_ALLOW_PAIRING_INITIATION`      | unset (disabled)                | Set exactly to `1`, `true`, or `TRUE` to allow the `freebox_pairing_initiate` tool. Startup bootstrap does not require this opt-in.                                                |
| `FREEBOX_SERVER_ADDRESS`                | `https://mafreebox.freebox.fr/` | Freebox server base address passed to the Freebox client.                                                                                                                          |
| `FREEBOX_PAIRING_POLL_INTERVAL_SECONDS` | `2`                             | Default polling interval (seconds) used by `freebox_pairing_wait` when no explicit argument is provided. Must be `> 0`.                                                            |
| `FREEBOX_PAIRING_WAIT_TIMEOUT_SECONDS`  | `600`                           | Default timeout (seconds) used by `freebox_pairing_wait` when no explicit argument is provided. Must be `> 0`.                                                                     |
| `FREEBOX_AUTHENTICATE_ON_STARTUP`       | `true`                          | Enables startup authentication bootstrap (reuse persisted credentials; if missing/expired, initiate pairing and wait for physical approval automatically).                         |
| `FREEBOX_CREDENTIAL_FILE`               | unset (OS vault)                | Optional path to an AES-256-GCM encrypted credential file (container/headless mode). When set, it takes priority over the OS vault; requires `FREEBOX_CREDENTIAL_PASSPHRASE_FILE`. |
| `FREEBOX_CREDENTIAL_PASSPHRASE_FILE`    | unset                           | Path to the file containing the passphrase used by PBKDF2-SHA256 (with per-file random salt) to derive the AES-256 key for `FREEBOX_CREDENTIAL_FILE`.                              |

Do not write application tokens or credentials into MCP configuration files, environment variables, or arguments.

## Tools and resources

Tools are organized by families: discovery, pairing, network, system, downloads, calls, cameras, contacts,
AirMedia, shares, parental, and notifications.

Resources: `freebox://docs/en/overview`, `freebox://docs/en/resources`, `freebox://docs/fr/overview`,
`freebox://docs/fr/resources`, and `freebox://live/status` (current sanitized status).

All status tools are read-only, sanitized and bounded: no hostnames, IP/MAC addresses, SSIDs or tokens are ever
returned; host identifiers are SHA-256 hashes.

## Pairing

1. `freebox_pairing_status` → state (`Missing` on first run).
2. Startup bootstrap (`FREEBOX_AUTHENTICATE_ON_STARTUP=true`) automatically initiates pairing when no valid token is
   available and waits for **physical approval on the Freebox display**.
3. `freebox_pairing_initiate` can still be used manually, but it remains gated by
   `FREEBOX_ALLOW_PAIRING_INITIATION=1|true|TRUE`.
4. Approve on the Freebox, then `freebox_pairing_wait` (optionally `pollIntervalSeconds` and `timeoutSeconds`) →
   `Approved`; the app token is stored encrypted in the OS
   credential vault.
5. Read-only tools (`freebox_connection_status`, `freebox_lan_overview`, `freebox_wifi_status`,
   `freebox_system_status`) now work.

The server intentionally has no CLI side-channel. Pairing and credential bootstrap are handled only through MCP tools
and startup automation.

## Security

See [Security/README.md](Security/README.md) for the full storage matrix and rules:

- Windows: Credential Manager (`CredWriteW`/`CredReadW`/`CredDeleteW`, DPAPI-encrypted, local machine).
- Linux: libsecret via the `secret-tool` CLI when the binary is on `PATH`.
- Containers/headless: AES-256-GCM encrypted file when `FREEBOX_CREDENTIAL_FILE` is set.
- Anything else: fail-closed, nothing persisted in plaintext.
- Storage key: SHA-256 of Freebox UID + application ID + profile. Session tokens stay in memory only (`X-Fbx-App-Auth`
  header). Secrets are redacted from logs (stderr only).

## Build and package (from the repository root)

```powershell
dotnet restore Freenaute.Freebox.MCP.slnx
dotnet build Freenaute.Freebox.MCP.slnx -c Release --no-restore
dotnet test Freenaute.Freebox.MCP.slnx -c Release

# Native AOT single-file publish, one RID (win-x64|win-arm64|linux-x64|linux-arm64)
dotnet publish mcp/Freenaute.Freebox.Mcp/Freenaute.Freebox.Mcp.csproj `
  -c Release -r win-x64 --self-contained true -p:PublishAot=true `
  --output .artifacts/publish/1.0.0/win-x64

# NuGet package (DotnetTool + McpServer, embeds .mcp/server.json and this README)
dotnet pack mcp/Freenaute.Freebox.Mcp/Freenaute.Freebox.Mcp.csproj -c Release   # -> .artifacts/package
```

CI/CD release automation (binaries, NuGet package, containers, SBOM, checksums):
[.github/workflows/release.yml](../../.github/workflows/release.yml)

`PublishAot`, single-file publishing, and invariant globalization are already set in the
project. Keep warnings from the AOT/trimming/single-file analyzers enabled, and smoke-test
the published artifact for every supported RID (`win-x64`, `win-arm64`, `linux-x64`,
`linux-arm64`) because Native AOT output is platform-specific. Avoid runtime reflection or
dynamic code generation in new registrations or tool metadata.

`Program.cs` is the host composition root: register feature services before `Build()`,
construct one host, and let DI own service disposal. The `AddFreebox...` extension methods
keep reusable registration bundles, options fail at startup via `ValidateOnStart()`, and
startup authentication is hosted through the normal host lifetime. Do not add
`BuildServiceProvider()` calls to registration code.

Microsoft Learn references:
[Native AOT deployment](https://learn.microsoft.com/dotnet/core/deploying/native-aot/),
[dependency injection overview](https://learn.microsoft.com/dotnet/core/extensions/dependency-injection/overview),
and [dependency injection guidelines](https://learn.microsoft.com/dotnet/core/extensions/dependency-injection/guidelines).

## MCP smoke test

The MCP stdio transport is newline-delimited JSON. Send this as one line on stdin:

```json
{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-06-18","capabilities":{},"clientInfo":{"name":"smoke-test","version":"1.0"}}}
```

A successful response contains the `Freenaute.Freebox.Mcp` server identity and the MCP `tools`/`resources`
capabilities.

## More information

- [Freebox OS API documentation](https://dev.freebox.fr/sdk/os/)
- [Model Context Protocol](https://modelcontextprotocol.io/)
- [MCP protocol specification](https://spec.modelcontextprotocol.io/)
- [.NET MCP SDK](https://csharp.sdk.modelcontextprotocol.io/)
- [NuGet MCP server guide](https://aka.ms/nuget/mcp/guide)
