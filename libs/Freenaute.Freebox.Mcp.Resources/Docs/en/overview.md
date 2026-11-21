# Freebox MCP server — overview (English)

You are connected to `Freenaute.Freebox.Mcp`, a **local stdio MCP server** that talks to a Freebox Server through the
Freebox OS API (via the `Freenaute.Freebox.Client` NuGet package). It offers **discovery, pairing coordination, and
read-only status** of the Freebox and its LAN/Wi-Fi. It cannot change Freebox configuration.

## Transport and runtime

- stdio only: JSON-RPC on stdin/stdout; logs on stderr. No TCP port is opened.
- .NET 11, Native AOT single-file; also distributed as a NuGet tool (`Freenaute.Freebox.Mcp`) runnable with `dnx`
  or `freenaute-freebox-mcp`.
- Server identity: `Freenaute.Freebox.Mcp` (version `0.1.0-beta`); package name in `.mcp/server.json`:
  `io.github.freenaute/freenaute-freebox-mcp`.

## Capabilities

8 tools (see `freebox://docs/en/resources` for details and examples):

- `freebox_discover` — reachable Freebox + API capability metadata (read-only).
- `freebox_pairing_status` — credential state, never secrets (read-only).
- `freebox_pairing_initiate` — start pairing; **gated** behind `FREEBOX_ALLOW_PAIRING_INITIATION=1|true|TRUE`.
- `freebox_pairing_wait` — wait for approval of an existing pairing; cancellable.
- `freebox_connection_status`, `freebox_lan_overview`, `freebox_wifi_status`, `freebox_system_status` — read-only,
  sanitized status.

5 resources: this documentation (`freebox://docs/en/*`, `freebox://docs/fr/*`) and `freebox://live/status`.

## Pairing workflow (important)

1. Call `freebox_pairing_status`. On a fresh machine the state is `Missing`.
2. If allowed by host policy, call `freebox_pairing_initiate`. It only runs when the operator set
   `FREEBOX_ALLOW_PAIRING_INITIATION` to exactly `1`, `true` or `TRUE`; otherwise it fails with a policy error.
3. **A human must physically approve the application on the Freebox display (LCD).** This is mandatory and cannot be
   bypassed by any tool or setting.
4. Call `freebox_pairing_wait` to poll until the state becomes `Approved` (or `Denied` / `Expired`). It is safe to
   cancel and resume.
5. Once `Approved`, the read-only status tools work. The durable app token is stored **encrypted in the OS
   credential vault** (Windows Credential Manager / macOS Keychain); on Linux/containers there is **no storage** and
   pairing fails closed. The session token lives in memory only (`X-Fbx-App-Auth` header).

Under the hood: `POST /api/v*/login/authorize/` → `app_token` + `track_id`; poll
`GET /api/v*/login/authorize/{track_id}` (`pending|granted|denied|timeout`); then `GET /api/v*/login/` → `challenge`;
`POST /api/v*/login/session/` with `password = HMAC-SHA1(app_token, challenge)` → `session_token`.

## Safety rules (always apply)

- **Read-only posture**: the 6 status/discovery tools never mutate the Freebox. Only the two pairing tools are
  non-read-only, and `freebox_pairing_initiate` is policy-gated.
- **Sanitized outputs**: tool results and `freebox://live/status` never contain hostnames, domains, IP or MAC
  addresses, SSIDs, BSSIDs, tokens, or challenges. Host/sensor/BSS identifiers are SHA-256 hex digests. Strings are
  stripped of control characters and length-capped; collections are capped (32 interfaces, 128 hosts/interface,
  32 access points, 128 BSS, 32 system components); resource contents are truncated at 64 KiB.
- **Secrets**: never ask the user to paste a token into chat, and never put a token in an argument, env var, config
  file, or log. Use `freenaute-freebox-mcp import-token --freebox-uid <uid>` which reads the token from stdin only.
- **Errors** come back as structured errors with stable codes: `AuthenticationRequired`, `AuthorizationPending`,
  `AuthorizationDenied`, `AuthorizationExpired`, `ApiRejected`, `TransportFailure`, `CredentialStoreUnavailable`,
  `Cancelled`, `InvalidRequest`. Messages are redacted.

## Limits

- One Freebox per server instance; address defaults to `https://mafreebox.freebox.fr/`.
- `freebox_lan_overview` returns hosts only when called with `includeHosts=true`, optionally filtered by
  `interfaceName` (1–64 chars, no control characters).
- If the credential store is unavailable (e.g. inside the Linux container), status tools still report
  `credential state: Unavailable` and pairing tools fail closed.
- Live resource reads have a 10 s timeout and are cancellable.

## Configuration (environment variables)

| Variable                           | Default                           | Purpose                                                |
|------------------------------------|-----------------------------------|--------------------------------------------------------|
| `FREEBOX_APPLICATION_ID`           | `io.github.freenaute.freebox-mcp` | Freebox application id.                                |
| `FREEBOX_APPLICATION_NAME`         | `Freenaute Freebox MCP`           | Name shown during pairing.                             |
| `FREEBOX_DEVICE_NAME`              | machine name                      | Device name shown during pairing.                      |
| `FREEBOX_CREDENTIAL_PROFILE`       | `default`                         | Credential profile (scoping).                          |
| `FREEBOX_ALLOW_PAIRING_INITIATION` | unset = disabled                  | `1`/`true`/`TRUE` to allow `freebox_pairing_initiate`. |
