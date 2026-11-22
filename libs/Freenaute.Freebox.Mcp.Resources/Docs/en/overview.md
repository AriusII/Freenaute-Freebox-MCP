# Freebox MCP server — overview (English)

You are connected to `Freenaute.Freebox.Mcp`, a **local stdio MCP server** that talks to a Freebox Server through the
Freebox OS API (via the `Freenaute.Freebox.Client` NuGet package). It offers **discovery, pairing coordination, and
read-only status** of the Freebox and its LAN/Wi-Fi. It cannot change Freebox configuration.

## Transport and runtime

- stdio only: JSON-RPC on stdin/stdout; logs on stderr. No TCP port is opened.
- .NET 11, Native AOT single-file; also distributed as a NuGet tool (`Freenaute.Freebox.Mcp`) runnable with `dnx`
  or `freenaute-freebox-mcp`.
- Server identity: `Freenaute.Freebox.Mcp` (version `1.0.0`); package name in `.mcp/server.json`:
  `org.freenaute/freenaute-freebox-mcp`.

## Capabilities

The server exposes 31 tools across operational families (see `freebox://docs/en/resources` for routing, limits,
and examples):

### Discovery

- `freebox_discover` — verifies reachability and returns bounded, non-identifying API capability metadata.

### Pairing

- `freebox_pairing_status` — reports credential state without exposing secrets.
- `freebox_pairing_initiate` — starts pairing only behind
  `FREEBOX_ALLOW_PAIRING_INITIATION=1|true|TRUE`; physical approval is still mandatory.
- `freebox_pairing_wait` — polls an existing pairing for approval; cancellable and resumable, with optional polling
  interval and timeout.

### Network

- `freebox_connection_status` — connection state, media, rates, counters, and remote-access flags.
- `freebox_lan_overview` — bounded interface summaries; hosts are optional and identifiers are hashed.
- `freebox_wifi_status` — bounded Wi-Fi and network state; SSIDs and BSSIDs are excluded.

### System

- `freebox_system_status` — firmware, model, uptime, disk health, sensors, fans, and expansion status.

### Downloads

- List, status, aggregate statistics, configuration, files, log, and piece state; all read-only. Task IDs are required
  for detail tools.

### Calls

- Read the bounded call log/account and optionally mark all entries read or clear the destructive call log.

### Contacts

- List or search bounded safe contact summaries. Raw names, numbers, and addresses are not exposed.

### Cameras

- Read bounded camera summaries and home language, standby planning, and update state. No streams or controls.

### Notifications

- Read bounded status and derived notification feeds. The MCP tools do not acknowledge or mutate notifications.

### AirMedia

- Read bounded AirMedia state and receiver capabilities. No casting or playback control is exposed.

### Shares

- Read NAS and LAN-share visibility. No share paths, credentials, or host identities are exposed.

### Parental controls

- Read default policy mode and bounded sanitized filter state. No filter targets or mutation operations are exposed.

Discovery, network, and system tools are read-only. Pairing status is also read-only; pairing initiation is explicitly
gated and pairing wait is a long-running approval operation.

Resources: embedded English/French documentation (`freebox://docs/en/*`, `freebox://docs/fr/*`) and the live
`freebox://live/status` resource.

## Pairing workflow (important)

1. Call `freebox_pairing_status`. On a fresh machine the state is `Missing`.
2. If allowed by host policy, call `freebox_pairing_initiate`. It only runs when the operator set
   `FREEBOX_ALLOW_PAIRING_INITIATION` to exactly `1`, `true` or `TRUE`; otherwise it fails with a policy error.
3. **A human must physically approve the application on the Freebox display (LCD).** This is mandatory and cannot be
   bypassed by any tool or setting.
4. Call `freebox_pairing_wait` to poll until the state becomes `Approved` (or `Denied` / `Expired`). It is safe to
   cancel and resume.
5. Once `Approved`, the read-only status tools work. The durable app token is stored in the OS credential vault (Windows
   Credential Manager / Linux Secret Service via `secret-tool` / macOS Keychain). Containers or other
   headless hosts may use the encrypted file backend when configured. If no secure storage is available, pairing
   fails closed. The session token lives in memory only (`X-Fbx-App-Auth` header).

Under the hood: `POST /api/v*/login/authorize/` → `app_token` + `track_id`; poll
`GET /api/v*/login/authorize/{track_id}` (`pending|granted|denied|timeout`); then `GET /api/v*/login/` → `challenge`;
`POST /api/v*/login/session/` with `password = HMAC-SHA1(app_token, challenge)` → `session_token`.

## Safety rules (always apply)

- **Read-only posture**: discovery, network, system, downloads, calls, contacts, cameras, notifications, AirMedia,
  shares, and parental read tools never mutate the Freebox. Pairing initiation/wait and call-log mutations are the
  explicitly non-read-only operations; pairing initiation is policy-gated and call-log clear is destructive.
- **Sanitized outputs**: tool results and `freebox://live/status` never contain hostnames, domains, IP or MAC
  addresses, SSIDs, BSSIDs, tokens, or challenges. Host/sensor/BSS identifiers are SHA-256 hex digests. Strings are
  stripped of control characters and length-capped; collections are capped (32 interfaces, 128 hosts/interface,
  32 access points, 128 BSS, 32 system components); resource contents are truncated at 64 KiB.
- **Secrets**: never ask the user to paste a token into chat, and never put a token in an argument, env var, config
  file, or log. The server accepts no command-line arguments; credentials are obtained through the MCP pairing flow.
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

| Variable                                | Default                     | Purpose                                                |
|-----------------------------------------|-----------------------------|--------------------------------------------------------|
| `FREEBOX_APPLICATION_ID`                | `org.freenaute.freebox-mcp` | Freebox application id.                                |
| `FREEBOX_APPLICATION_NAME`              | `Freenaute Freebox MCP`     | Name shown during pairing.                             |
| `FREEBOX_DEVICE_NAME`                   | machine name                | Device name shown during pairing.                      |
| `FREEBOX_CREDENTIAL_PROFILE`            | `default`                   | Credential profile (scoping).                          |
| `FREEBOX_ALLOW_PAIRING_INITIATION`      | unset = disabled            | `1`/`true`/`TRUE` to allow `freebox_pairing_initiate`. |
| `FREEBOX_PAIRING_POLL_INTERVAL_SECONDS` | `2`                         | Default pairing poll interval; must be `> 0`.          |
| `FREEBOX_PAIRING_WAIT_TIMEOUT_SECONDS`  | `600`                       | Default pairing wait timeout; must be `> 0`.           |
| `FREEBOX_AUTHENTICATE_ON_STARTUP`       | `true`                      | Reuse stored credentials and bootstrap authentication. |
