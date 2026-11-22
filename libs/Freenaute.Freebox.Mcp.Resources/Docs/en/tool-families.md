# Freebox MCP tool-family guide (English)

Use this guide for the read-only operational families not covered by the pairing and live-status workflows.
The server remains local-only and stdio-only. Prefer narrow tools and small limits; do not treat a bounded result
as a complete inventory. Official Freebox OS API scope: https://dev.freebox.fr/sdk/os/

## Downloads family

Tools:

- `freebox_downloads_list` — list at most 128 sanitized download tasks.
- `freebox_download_status` — inspect one task by positive numeric `id`.
- `freebox_download_stats` — aggregate task/RSS counts and transfer rates.
- `freebox_download_configuration` — read throttling and task limits.
- `freebox_download_files` — list at most 512 sanitized files for a task.
- `freebox_download_log` — read a task log, capped at 4096 characters.
- `freebox_download_pieces` — read piece-state text, capped at 4096 characters.

Routing: use list/status for task selection, stats for aggregate questions, files/log/pieces only after selecting an
`id`, and configuration for policy state. Names and status text are sanitized. Paths, file hashes, credentials,
passwords, watch/download directories, and preview URLs are not returned.

Official area: https://dev.freebox.fr/sdk/os/downloads/

## Calls family

Tools:

- `freebox_calls_log` — return at most 128 bounded call entries.
- `freebox_calls_account` — return a hashed account identifier rather than a phone number.
- `freebox_calls_mark_all_read` — mark the call log read.
- `freebox_calls_clear_log` — destructively delete the complete call log.

Routing: use the log for call state and dates, not to reconstruct a person's identity. The account and call,
contact, and line identifiers are hashed. Phone numbers and names are excluded.

Mutating tools require explicit user intent. Confirm “mark all read” or “clear the entire call log” before calling;
restate the destructive consequence of `freebox_calls_clear_log`. Both mutations are policy-gated by normal Freebox
authentication and the log-clear operation is intentionally irreversible in the Freebox.

Official area: https://dev.freebox.fr/sdk/os/call/

## Contacts family

Tools:

- `freebox_contacts_list` — optionally filter with `query` and return 1–64 results (default 25).
- `freebox_contacts_search` — require a 1–128-character, control-free `query` and return 1–64 results.

Routing: search is preferable for a known name or company; list can accept an optional filter. Keep `limit` small
and summarize the count without attempting to expand or infer private details. Returned contacts contain only safe
summary fields. Do not request raw numbers, addresses, or synchronization payloads: the MCP tools do not expose them.

Official area: https://dev.freebox.fr/sdk/os/contacts/

## Cameras family

Tools:

- `freebox_cameras_overview` — return at most 32 bounded camera summaries.
- `freebox_home_status` — return bounded language, standby/planning, and update states.

Routing: use cameras for camera inventory/state questions and home status for standby planning or upgrade state.
Camera names, LAN IDs, and stream URLs are excluded. Language and planning collections are capped.

Do not infer physical identity or location from a camera entry. These tools are read-only: they do not pan, control
night vision, retrieve streams, or change standby/update configuration.

Official area: https://dev.freebox.fr/sdk/os/camera/

## Notifications family

Tools:

- `freebox_status_feed` — return at most 8 current pairing, connection, LAN, Wi-Fi, and system entries.
- `freebox_notifications_feed` — return at most 16 sanitized actionable notifications derived from current status.

Routing: use status feed for a compact health summary and notifications feed for alert/action candidates. Feed
entries are derived snapshots, not durable event storage, and timestamps/status values must be presented as observed.
Do not claim that a notification was acknowledged or resolved: these tools do not mutate notification state.

Official context: https://dev.freebox.fr/sdk/os/

## AirMedia family

Tools:

- `freebox_airmedia_status` — combine enabled state and up to 32 sanitized receiver summaries.
- `freebox_airmedia_receivers` — list the same bounded receiver summaries without the enabled-state field.

Routing: use status for enabled/configured questions and receivers for capability inventory. Receiver names are
length-capped and password-protection/capability flags are preserved. Do not expose or guess the receiver password.
These tools are read-only and do not start playback or cast content.

Official area: https://dev.freebox.fr/sdk/os/airmedia/

## Shares family

Tools:

- `freebox_nas_visibility` — read storage power/disk/model/firmware visibility.
- `freebox_shares_visibility` — summarize LAN share interface visibility.

Routing: use NAS visibility for storage reachability/health and shares visibility for interface counts. Set
`includeHosts=true` only for a host count; set `interfaceName` (1–64 characters, no controls) only to narrow a named
interface. Host identifiers are not returned and share names, paths, credentials, and user identities are excluded.

Official area: https://dev.freebox.fr/sdk/os/share/

## Parental controls family

Tools:

- `freebox_parental_config` — read the default sanitized filter mode.
- `freebox_parental_filters` — read at most 128 filters; default limit is 64.

Routing: use config for the global mode and filters for policy-state summaries. MACs, host names, IP addresses,
descriptions, and raw targets are excluded. Filter entries retain hashed IDs and bounded state/mode flags only.

Do not reinterpret a filter as evidence of a person's identity or location. These tools are read-only and do not
add, change, enable, or schedule filters.

Official area: https://dev.freebox.fr/sdk/os/parental-control/

## MCP-safe usage rules

- Local-only: never send Freebox API traffic through a public listener or relay. The MCP server uses stdio.
- Authentication: stop and explain policy/storage errors; never ask for or accept app/session tokens in chat.
- Sanitization: preserve only bounded, redacted fields; control characters are removed and strings are length-capped.
- Stable identifiers: if an identifier is present, treat its SHA-256 hex value as opaque and never try to reverse it.
- Bounds: collection limits are output limits, not evidence that the Freebox has no additional entries.
- Mutations: only call-log mutations exist in this family. Require explicit intent and confirm destructive effects.
- Official context: verify endpoint behavior and policy against the official Freebox OS SDK, but never copy raw secret
  or identifying payload fields into MCP output or logs.
