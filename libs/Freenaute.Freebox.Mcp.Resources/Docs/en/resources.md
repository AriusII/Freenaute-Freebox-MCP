# Resource and tool catalog (English)

This document is served by the MCP server as `freebox://docs/en/resources` (and its French counterpart
`freebox://docs/fr/resources`). Use it to decide which tool/resource to call.

## MCP resources

| URI                           | MIME          | What it contains                                                                                                                   |
|-------------------------------|---------------|------------------------------------------------------------------------------------------------------------------------------------|
| `freebox://docs/en/overview`  | text/markdown | Server overview, pairing workflow, safety rules, limits, env vars.                                                                 |
| `freebox://docs/en/resources` | text/markdown | This catalog: tools, parameters, output formats, examples.                                                                         |
| `freebox://docs/fr/overview`  | text/markdown | French version of the overview.                                                                                                    |
| `freebox://docs/fr/resources` | text/markdown | French version of this catalog.                                                                                                    |
| `freebox://live/status`       | text/markdown | Live sanitized snapshot: sections `## Pairing`, `## Network`, `## System` (bounded counts, sanitized tokens, truncated at 64 KiB). |

Documentation resources are static embedded markdown; the live resource is produced through
`IFreeboxLiveResourceSource` with a 10 s timeout.

## MCP tools

| Tool                        | Read-only  | Idempotent | Parameters                                                                                      | Returns (structured content)                                                                                                                                                                                                                                              |
|-----------------------------|------------|------------|-------------------------------------------------------------------------------------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `freebox_discover`          | yes        | yes        | —                                                                                               | `boxModel` (≤64 chars), `apiVersion` (≤32), `httpsAvailable`, `httpsPort`.                                                                                                                                                                                                |
| `freebox_pairing_status`    | yes        | yes        | —                                                                                               | `credentialStoreAvailable`, `credentialState`: `Missing\|Pending\|Approved\|Denied\|Expired\|Corrupt\|Unavailable`.                                                                                                                                                       |
| `freebox_pairing_initiate`  | no (gated) | no         | —                                                                                               | `initiated`, `state`, `message`. Fails unless `FREEBOX_ALLOW_PAIRING_INITIATION` is `1`/`true`/`TRUE`.                                                                                                                                                                    |
| `freebox_pairing_wait`      | no         | yes        | —                                                                                               | `approved`, `state`, `message`. Poll until `Approved`/`Denied`/`Expired`; cancellable.                                                                                                                                                                                    |
| `freebox_connection_status` | yes        | yes        | —                                                                                               | `state`, `type`, `media`, `rateUp`, `rateDown`, `bandwidthUp`, `bandwidthDown`, `bytesUp`, `bytesDown`, `pingEnabled`, `apiRemoteAccessEnabled`, `remoteAccessEnabled`, `wakeOnLanEnabled`, `sipAlg`. **No IP addresses.**                                                |
| `freebox_lan_overview`      | yes        | yes        | `includeHosts` (bool, default `false`), `interfaceName` (string?, 1–64 chars, no control chars) | `name` (≤64), `type`, `mode`, `interfaces[]`: `name` (≤64), `hostCount`, optional `hosts[]`: `id` (SHA-256 hex), `hostType` (≤32), `reachable`, `active`, `lastActivity`, `interface` (≤64). **No hostnames, domains, or addresses.**                                     |
| `freebox_wifi_status`       | yes        | yes        | —                                                                                               | `enabled`, `macFilterState`, `state`, `accessPoints[]` (≤32): `id`, `state` (≤32); `networks[]` (≤128): `id` (SHA-256 hex), `phyId`, `state` (≤32), `stationCount`, `authorizedStationCount`, `enabled`, `encryption` (≤32), `wpsEnabled`. **No SSIDs or BSSIDs.**        |
| `freebox_system_status`     | yes        | yes        | —                                                                                               | `firmwareVersion` (≤32), `model` (≤64), `boardName` (≤64), `uptimeSeconds`, `boxAuthenticated`, `usb3Enabled`, `userStoragePowered`, `diskStatus`, `sensors[]`/`fans[]` (≤32): `id` (SHA-256 hex), `value`; `expansions[]` (≤32): `slot`, `present`, `supported`, `type`. |

All tools are `Destructive = false`, `OpenWorld = true`, and return typed structured content plus JSON text.

## Output format and bounds

- Every tool returns a result object; on failure an error object with a stable `code`
  (`AuthenticationRequired`, `AuthorizationPending`, `AuthorizationDenied`, `AuthorizationExpired`, `ApiRejected`,
  `TransportFailure`, `CredentialStoreUnavailable`, `Cancelled`, `InvalidRequest`, `Unknown`), a **redacted** message,
  optional HTTP status, and a correlation id.
- Sanitization: control characters stripped, strings truncated at per-field caps, collections capped as listed above,
  identifiers replaced by lowercase SHA-256 hex.
- `freebox://live/status` markdown: `## Pairing` (credential store availability + state), `## Network` (connection
  state/type/media, rates, byte counters, Wi-Fi enabled/state, AP and network counts), `## System` (uptime,
  authenticated, disk status, sensor/fan/expansion counts). Values are sanitized to `[A-Za-z0-9_.-]` ≤ 32 chars.

## Example requests (typical conversation)

1. "Is my Freebox reachable?" → `freebox_discover`
2. "Are we paired?" → `freebox_pairing_status` → if `Missing`: explain that pairing needs the opt-in env var **and**
   physical approval on the Freebox display, then `freebox_pairing_initiate` + `freebox_pairing_wait`.
3. "What is my connection speed?" → `freebox_connection_status` (`rateUp`/`rateDown` in bytes/s, plus byte counters).
4. "Who is on my LAN?" → `freebox_lan_overview` with `includeHosts=true` (hosts appear as hashed ids with
   reachable/active flags; names and addresses are intentionally absent).
5. "Show my Wi-Fi networks" → `freebox_wifi_status` (counts per network, encryption, WPS; SSIDs are intentionally
   absent).
6. "Give me a full current snapshot" → read resource `freebox://live/status`.
7. "Is the box healthy?" → `freebox_system_status` (firmware, uptime, disk status, sensors/fans).

## Good practices for the LLM

- Prefer `freebox://live/status` for a one-shot overview; use individual tools for precise questions.
- Never attempt to exfiltrate identifiers: outputs are hashed by design; do not try to reverse them.
- Never ask the user for their app token in chat; point them to `freenaute-freebox-mcp import-token --freebox-uid <uid>`
  (stdin only) if they already paired elsewhere.
- If a tool returns `CredentialStoreUnavailable` (e.g. Linux container), explain that pairing must be done on a host
  with an OS vault (Windows/macOS) — the server fails closed by design.
- `freebox_pairing_wait` can run long; it is cancellable — cancel it if the user says the approval timed out on the
  Freebox.
