# Resource and tool catalog (English)

This document is served by the MCP server as `freebox://docs/en/resources` (and its French counterpart
`freebox://docs/fr/resources`). Use it to decide which tool/resource to call.

## MCP resources

| URI                                            | MIME          | What it contains                                                                                                                   |
|------------------------------------------------|---------------|------------------------------------------------------------------------------------------------------------------------------------|
| `freebox://docs/en/overview`                   | text/markdown | Server overview, pairing workflow, safety rules, limits, env vars.                                                                 |
| `freebox://docs/en/resources`                  | text/markdown | This catalog: tools, parameters, output formats, examples.                                                                         |
| `freebox://docs/en/quickstart`                 | text/markdown | English local-only MCP routing quickstart.                                                                                         |
| `freebox://docs/en/auth-pairing`               | text/markdown | Freebox OS authentication and pairing workflow.                                                                                    |
| `freebox://docs/fr/overview`                   | text/markdown | French version of the overview.                                                                                                    |
| `freebox://docs/fr/resources`                  | text/markdown | French version of this catalog.                                                                                                    |
| `freebox://docs/fr/quickstart`                 | text/markdown | French local-only MCP routing quickstart.                                                                                          |
| `freebox://docs/fr/authentification-appairage` | text/markdown | French Freebox OS authentication and pairing workflow.                                                                             |
| `freebox://live/status`                        | text/markdown | Live sanitized snapshot: sections `## Pairing`, `## Network`, `## System` (bounded counts, sanitized tokens, truncated at 64 KiB). |

Documentation resources are static embedded markdown; the live resource is produced through
`IFreeboxLiveResourceSource` with a 10 s timeout.

## MCP tools

| Tool                        | Read-only  | Idempotent | Parameters                                                                                      | Returns (structured content)                                                                                                                                                                                                                                              |
|-----------------------------|------------|------------|-------------------------------------------------------------------------------------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `freebox_discover`          | yes        | yes        | —                                                                                               | `boxModel` (≤64 chars), `apiVersion` (≤32), `httpsAvailable`, `httpsPort`.                                                                                                                                                                                                |
| `freebox_pairing_status`    | yes        | yes        | —                                                                                               | `credentialStoreAvailable`, `credentialState`: `Missing\|Pending\|Approved\|Denied\|Expired\|Corrupt\|Unavailable`.                                                                                                                                                       |
| `freebox_pairing_initiate`  | no (gated) | no         | —                                                                                               | `initiated`, `state`, `message`. Fails unless `FREEBOX_ALLOW_PAIRING_INITIATION` is `1`/`true`/`TRUE`.                                                                                                                                                                    |
| `freebox_pairing_wait`      | no         | yes        | `pollIntervalSeconds` (int?, >0), `timeoutSeconds` (int?, >0)                                   | `approved`, `state`, `message`. Poll until `Approved`/`Denied`/`Expired`; cancellable.                                                                                                                                                                                    |
| `freebox_connection_status` | yes        | yes        | —                                                                                               | `state`, `type`, `media`, `rateUp`, `rateDown`, `bandwidthUp`, `bandwidthDown`, `bytesUp`, `bytesDown`, `pingEnabled`, `apiRemoteAccessEnabled`, `remoteAccessEnabled`, `wakeOnLanEnabled`, `sipAlg`. **No IP addresses.**                                                |
| `freebox_lan_overview`      | yes        | yes        | `includeHosts` (bool, default `false`), `interfaceName` (string?, 1–64 chars, no control chars) | `name` (≤64), `type`, `mode`, `interfaces[]`: `name` (≤64), `hostCount`, optional `hosts[]`: `id` (SHA-256 hex), `hostType` (≤32), `reachable`, `active`, `lastActivity`, `interface` (≤64). **No hostnames, domains, or addresses.**                                     |
| `freebox_wifi_status`       | yes        | yes        | —                                                                                               | `enabled`, `macFilterState`, `state`, `accessPoints[]` (≤32): `id`, `state` (≤32); `networks[]` (≤128): `id` (SHA-256 hex), `phyId`, `state` (≤32), `stationCount`, `authorizedStationCount`, `enabled`, `encryption` (≤32), `wpsEnabled`. **No SSIDs or BSSIDs.**        |
| `freebox_system_status`     | yes        | yes        | —                                                                                               | `firmwareVersion` (≤32), `model` (≤64), `boardName` (≤64), `uptimeSeconds`, `boxAuthenticated`, `usb3Enabled`, `userStoragePowered`, `diskStatus`, `sensors[]`/`fans[]` (≤32): `id` (SHA-256 hex), `value`; `expansions[]` (≤32): `slot`, `present`, `supported`, `type`. |

All tools are `Destructive = false`, `OpenWorld = true`, and return typed structured content plus JSON text.

## Tool families

### Discovery family

Use `freebox_discover` first when the client needs to verify server reachability or API capability metadata.

- Purpose: confirm that the configured Freebox is reachable.
- Returns: bounded box model, API version, HTTPS availability, and HTTPS port.
- Constraints: read-only and idempotent; no address or other identifying network data is returned.
- Example: “Is `mafreebox.freebox.fr` reachable?” → call `freebox_discover`.

### Pairing family

Use pairing tools only for credential lifecycle coordination.

- `freebox_pairing_status`: read-only state check. States include `Missing`, `Pending`, `Approved`, `Denied`,
  `Expired`, `Corrupt`, and `Unavailable`.
- `freebox_pairing_initiate`: one-time start operation. It fails closed unless
  `FREEBOX_ALLOW_PAIRING_INITIATION` is exactly `1`, `true`, or `TRUE`. A user must approve on the physical display.
- `freebox_pairing_wait`: wait operation for an existing pairing. Accept `pollIntervalSeconds` and `timeoutSeconds`,
  each positive and optional. It is cancellable and resumable. Stop at `Approved`, `Denied`, or `Expired`.

Example sequence:

1. Call `freebox_pairing_status`.
2. If `Missing`, explain the opt-in and physical approval requirement.
3. Call `freebox_pairing_initiate` only if policy allows.
4. Call `freebox_pairing_wait`; cancel instead of continuing if the user abandons approval.
5. Call status tools only after `Approved`.

Never ask for or accept an app token as a tool argument or chat text.

### Network family

- `freebox_connection_status`: read-only Internet link state and traffic counters. Rates and counters are numeric;
  public/private IP addresses are excluded.
- `freebox_lan_overview`: read-only LAN summary. Set `includeHosts=true` only when host inventory is relevant.
  Optionally set `interfaceName` to 1–64 characters with no control characters.
- `freebox_wifi_status`: read-only Wi-Fi summary. SSIDs and BSSIDs are excluded; access-point and BSS identifiers may
  appear only as SHA-256 digests.

Constraints:

- LAN host details are intentionally limited to 128 hosts per interface and 32 interfaces.
- Wi-Fi output is limited to 32 access points and 128 BSS entries.
- Prefer the live resource for a broad snapshot, and individual tools for exact questions.
- Do not infer a real person or device name from a hashed identifier.

Example requests:

- “What is the Internet link state?” → `freebox_connection_status`.
- “How many hosts are visible on the LAN?” → `freebox_lan_overview` with `includeHosts=false`.
- “List connected hosts on the main interface” → `freebox_lan_overview` with `includeHosts=true` and a valid
  `interfaceName`.
- “Is Wi-Fi enabled and how many stations are associated?” → `freebox_wifi_status`.

### System family

Use `freebox_system_status` for bounded firmware, uptime, board, disk, authentication, USB, sensor, fan, and expansion
health.

- It is read-only and idempotent.
- Sensor, fan, and expansion collections are capped at 32 entries.
- Sensor/fan identifiers are hashed; expansion fields are sanitized.
- It is not a hardware-control or reboot tool.

Examples:

- “What firmware is running?” → read `firmwareVersion` and `model`.
- “How long has the box been up?” → read `uptimeSeconds`.
- “Is storage healthy?” → read `diskStatus` and the bounded component collections.

### Downloads family

- `freebox_downloads_list`: bounded task inventory; exclude paths, hashes, credentials, and passwords.
- `freebox_download_status`: one task by positive numeric `id`.
- `freebox_download_stats`: aggregate task/RSS counts and transfer rates.
- `freebox_download_configuration`: sanitized throttling and task limits.
- `freebox_download_files`: at most 512 files for a task; no paths or preview URLs.
- `freebox_download_log`: sanitized task log, capped at 4096 characters.
- `freebox_download_pieces`: sanitized piece state, capped at 4096 characters.

Use a task ID from list/status before requesting detail tools. Never infer a local filename or download location from
the bounded fields.

### Calls family

- `freebox_calls_log`: at most 128 call entries with no names or phone numbers.
- `freebox_calls_account`: hashed telephony-account identifier.
- `freebox_calls_mark_all_read`: explicit mutation to mark the full log read.
- `freebox_calls_clear_log`: explicitly destructive deletion of the complete call log.

Confirm destructive intent before clearing. Call identifiers and contact/line identifiers are opaque hashes.

### Contacts family

- `freebox_contacts_list`: optional `query` filter and a limit of 1–64 (default 25).
- `freebox_contacts_search`: required 1–128-character control-free `query` and a limit of 1–64 (default 25).

Use small limits and safe summary fields only. Raw phone numbers, addresses, and synchronization payloads are absent.

### Cameras family

- `freebox_cameras_overview`: at most 32 sanitized camera summaries; no names, LAN IDs, or stream URLs.
- `freebox_home_status`: bounded language, standby planning, and update state.

These tools are read-only. Do not infer a person or location from an entry and do not attempt camera control.

### Notifications family

- `freebox_status_feed`: at most 8 derived pairing, connection, LAN, Wi-Fi, and system entries.
- `freebox_notifications_feed`: at most 16 sanitized, actionable status-derived notifications.

Feeds are snapshots, not a durable event queue. Do not claim acknowledgment or resolution; the tools do not mutate
notification state.

### AirMedia family

- `freebox_airmedia_status`: enabled state plus at most 32 sanitized receiver summaries.
- `freebox_airmedia_receivers`: at most 32 receiver names and capability flags.

Receiver names and capability fields are bounded. Passwords are excluded. These tools do not cast or play content.

### Shares family

- `freebox_nas_visibility`: bounded storage power/disk/model/firmware state.
- `freebox_shares_visibility`: bounded LAN interface and share visibility; optionally include host counters.

Use `includeHosts=false` for interface-level status and set `interfaceName` only to a valid 1–64-character control-free
name. Share paths, credentials, host identifiers, and names are excluded.

### Parental controls family

- `freebox_parental_config`: sanitized default filter mode.
- `freebox_parental_filters`: at most 128 filters (default limit 64).

MACs, host names, IP addresses, descriptions, and raw targets are excluded. Filter IDs are opaque hashed identifiers;
the tools are read-only and cannot change a policy or schedule.

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
- Never ask the user for their app token in chat. The server has no token-import CLI path; direct users to the
  pairing tools workflow only.
- If a tool returns `CredentialStoreUnavailable` (e.g. Linux container), explain that pairing must be done on a host
  with an OS vault (Windows/macOS) — the server fails closed by design.
- `freebox_pairing_wait` can run long; it is cancellable — cancel it if the user says the approval timed out on the
  Freebox.
