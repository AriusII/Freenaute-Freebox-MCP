# Freebox MCP quickstart (EN)

## Scope

This MCP server is local-only and stdio-only. It focuses on safe, bounded discovery, pairing, network, and system
status workflows. It does not configure the Freebox or expose a network listener.

## Typical flow

1. `freebox_discover` — verify that a Freebox is reachable and identify its API capabilities.
2. `freebox_pairing_status` — inspect credential state without requesting a token.
3. If needed and local policy allows: `freebox_pairing_initiate`.
4. Have a human approve the application on the Freebox display, then call `freebox_pairing_wait`.
5. Use the network or system tools for the requested status.

## Tool-family routing

| User goal                              | Preferred tool or resource                              | Safe usage                                                                              |
|----------------------------------------|---------------------------------------------------------|-----------------------------------------------------------------------------------------|
| Check whether the Freebox is reachable | `freebox_discover`                                      | No parameters; repeatable.                                                              |
| Check credential state                 | `freebox_pairing_status`                                | Never request or print an app token.                                                    |
| Start or complete pairing              | `freebox_pairing_initiate`, `freebox_pairing_wait`      | Initiation requires opt-in policy; approval requires a human at the Freebox.            |
| Check Internet link or traffic         | `freebox_connection_status`                             | Read state, rates, and counters; IP values are excluded.                                |
| Inspect LAN inventory                  | `freebox_lan_overview`                                  | Keep `includeHosts=false` for interface counts; set it only when host data is relevant. |
| Review Wi-Fi                           | `freebox_wifi_status`                                   | Use counts and state fields; SSIDs/BSSIDs are intentionally absent.                     |
| Review hardware/firmware health        | `freebox_system_status`                                 | Interpret bounded sensor/fan/expansion data and disk status.                            |
| Review downloads                       | `freebox_downloads_list`, `freebox_download_stats`      | Use task IDs only for status/files/log/pieces; paths and credentials are excluded.      |
| Review call log or account             | `freebox_calls_log`, `freebox_calls_account`            | Names, numbers, and raw identifiers are excluded; confirm before read-log mutations.    |
| Find a contact                         | `freebox_contacts_search`, `freebox_contacts_list`      | Use small limits and safe summaries; raw contact payloads are unavailable.              |
| Review cameras or home state           | `freebox_cameras_overview`, `freebox_home_status`       | Read-only; names, stream URLs, and LAN IDs are excluded.                                |
| Read status or alerts                  | `freebox_status_feed`, `freebox_notifications_feed`     | Feeds are bounded derived snapshots, not mutable event queues.                          |
| Review AirMedia receivers              | `freebox_airmedia_status`, `freebox_airmedia_receivers` | Read-only; never request or print receiver passwords.                                   |
| Review NAS or LAN shares               | `freebox_nas_visibility`, `freebox_shares_visibility`   | No share paths or host identities; use host counts only when needed.                    |
| Review parental policy                 | `freebox_parental_config`, `freebox_parental_filters`   | Read-only; targets, MACs, IPs, names, and descriptions are excluded.                    |
| Return a compact one-shot snapshot     | `freebox://live/status`                                 | Prefer this for overview questions, then call precise tools for details.                |

## Pairing wait constraints

`freebox_pairing_wait` accepts optional integer arguments:

- `pollIntervalSeconds` must be greater than 0.
- `timeoutSeconds` must be greater than 0.
- Both may be omitted; host defaults apply.
- The call is cancellable. Cancel instead of retrying if the user stops the approval process or the device prompt
  expires.

Stop on `Approved`, `Denied`, or `Expired`. Do not claim pairing succeeded until the result state is `Approved`.

## Startup behavior

When `FREEBOX_AUTHENTICATE_ON_STARTUP=true` (default), the server tries to reuse persisted credentials at startup.
If credentials are missing and pairing initiation is disabled, it reports actionable status instead of forcing pairing.
Startup behavior never bypasses physical LCD approval.

## Safe filtering

- `freebox_lan_overview` accepts `interfaceName` (1–64 characters, no control characters). Use it to narrow interface
  summaries, not to search for host names or addresses.
- Host and Wi-Fi identifiers are hashed. Do not attempt to reverse or exfiltrate them.
- Collection results are bounded; an omitted or capped field is a deliberate output limit, not a complete inventory.

## Safety rules

- Never provide tokens in chat, arguments, or environment values.
- Pairing always requires physical approval on the Freebox display.
- Outputs are sanitized and bounded.
