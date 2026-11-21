<!-- mcp-name: org.freenaute/freenaute-freebox-mcp -->

# Freenaute Freebox MCP

Local **stdio** Model Context Protocol server for Freebox discovery, pairing coordination, and read-only
system/network status. Pairing always requires explicit **physical approval on the Freebox display**.

- Package: `Freenaute.Freebox.Mcp` (NuGet, package types `DotnetTool` + `McpServer`)
- MCP server name (`.mcp/server.json`): `org.freenaute/freenaute-freebox-mcp`
- Tool command: `freenaute-freebox-mcp`
- Runtime: .NET 11 (`net11.0`), C# 15, Native AOT, self-contained, single-file, invariant globalization
- Transport: **stdio only** — JSON-RPC on stdin/stdout, all logs on stderr

## Install

```powershell
# .NET global tool
dotnet tool install --global Freenaute.Freebox.Mcp

# or run without installing (dnx)
dnx Freenaute.Freebox.Mcp --version 0.1.0-beta --yes
```

## Use from an MCP client

```json
{
  "mcpServers": {
    "freenaute-freebox-mcp": {
      "type": "stdio",
      "command": "freenaute-freebox-mcp",
      "env": {
        "FREEBOX_ALLOW_PAIRING_INITIATION": "true"
      }
    }
  }
}
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

Or from source:

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

## Configuration (environment variables)

All optional; declared in `.mcp/server.json`:

| Variable                             | Default                         | Meaning                                                                                                                                                                            |
|--------------------------------------|---------------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `FREEBOX_APPLICATION_ID`             | `org.freenaute.freebox-mcp`     | Registered Freebox application identifier.                                                                                                                                         |
| `FREEBOX_APPLICATION_NAME`           | `Freenaute Freebox MCP`         | Application name shown during pairing.                                                                                                                                             |
| `FREEBOX_DEVICE_NAME`                | machine name                    | Device name shown during pairing.                                                                                                                                                  |
| `FREEBOX_CREDENTIAL_PROFILE`         | `default`                       | Credential profile; scopes the stored credential with the application ID and Freebox UID.                                                                                          |
| `FREEBOX_ALLOW_PAIRING_INITIATION`   | unset (disabled)                | Set exactly to `1`, `true`, or `TRUE` to allow `freebox_pairing_initiate`. Waiting on an already-started pairing (`freebox_pairing_wait`) remains possible without this opt-in.    |
| `FREEBOX_SERVER_ADDRESS`             | `https://mafreebox.freebox.fr/` | Freebox server base address passed to the Freebox client.                                                                                                                          |
| `FREEBOX_CREDENTIAL_FILE`            | unset (OS vault)                | Optional path to an AES-256-GCM encrypted credential file (container/headless mode). When set, it takes priority over the OS vault; requires `FREEBOX_CREDENTIAL_PASSPHRASE_FILE`. |
| `FREEBOX_CREDENTIAL_PASSPHRASE_FILE` | unset                           | Path to the file containing the passphrase used to derive the AES-256 key for `FREEBOX_CREDENTIAL_FILE`.                                                                           |

Do not write application tokens or credentials into MCP configuration files, environment variables, or arguments.

## Tools and resources

Tools (8): `freebox_discover`, `freebox_pairing_status`, `freebox_pairing_initiate` (gated),
`freebox_pairing_wait` (cancellable), `freebox_connection_status`, `freebox_lan_overview`,
`freebox_wifi_status`, `freebox_system_status`.

Resources: `freebox://docs/en/overview`, `freebox://docs/en/resources`, `freebox://docs/fr/overview`,
`freebox://docs/fr/resources`, and `freebox://live/status` (current sanitized status).

All status tools are read-only, sanitized and bounded: no hostnames, IP/MAC addresses, SSIDs or tokens are ever
returned; host identifiers are SHA-256 hashes.

## Pairing

1. `freebox_pairing_status` → state (`Missing` on first run).
2. `freebox_pairing_initiate` (only with `FREEBOX_ALLOW_PAIRING_INITIATION=1|true|TRUE`) → the Freebox asks for
   **physical approval on its display**.
3. Approve on the Freebox, then `freebox_pairing_wait` → `Approved`; the app token is stored encrypted in the OS
   credential vault.
4. Read-only tools (`freebox_connection_status`, `freebox_lan_overview`, `freebox_wifi_status`,
   `freebox_system_status`) now work.

## Import an existing application token

For a previously paired Freebox application, import its token directly into the operating system credential vault:

```powershell
freenaute-freebox-mcp import-token --freebox-uid <freebox-uid> --profile default
```

At an interactive terminal, the token prompt is hidden. In automation, redirect **protected stdin** (a pipe or
inherited file descriptor containing only the token) — never a token command-line argument (a `--token` argument is
rejected), environment variable, MCP JSON field, or log. The command accepts one token line and stores it
immediately, then performs a read-back verification without printing the token.

Optional scope switches are `--application-id <id>` and `--profile <name>`; defaults match
`FREEBOX_APPLICATION_ID` (falling back to `org.freenaute.freebox-mcp`) and `FREEBOX_CREDENTIAL_PROFILE` (`default`).
Use the same profile, application ID, and Freebox UID when the server runs: tokens are scoped by all three values.

Example using a PowerShell secure prompt converted only for the lifetime of one protected pipe:

```powershell
$token = Read-Host -AsSecureString "Freebox app token"
$plain = [System.Net.NetworkCredential]::new('', $token).Password
$plain | & freenaute-freebox-mcp import-token --freebox-uid <freebox-uid>
Remove-Variable plain, token
```

Do not save the token in a plaintext file, shell script, history entry, MCP configuration, CI log, or environment
variable. Windows uses Credential Manager; Linux uses libsecret through the `secret-tool` CLI when available;
containers/headless hosts can use the encrypted-file mode (`FREEBOX_CREDENTIAL_FILE` +
`FREEBOX_CREDENTIAL_PASSPHRASE_FILE`, AES-256-GCM). Import **fails closed** on platforms without a supported secure
storage; it never falls back to plaintext file storage.

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
  --output .artifacts/publish/0.1.0-beta/win-x64

# NuGet package (DotnetTool + McpServer, embeds .mcp/server.json and this README)
dotnet pack mcp/Freenaute.Freebox.Mcp/Freenaute.Freebox.Mcp.csproj -c Release   # -> .artifacts/package
```

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
