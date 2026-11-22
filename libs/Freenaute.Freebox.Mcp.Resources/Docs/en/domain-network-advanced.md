# Advanced network domain

Use this domain for detailed read-only networking questions. Routing and advanced configuration are outside the
safe write scope of the current tools.

## Operations

- `freebox_connection_status`: Internet link state, type, media, traffic counters, and feature flags.
- `freebox_lan_overview`: bounded LAN interfaces and optional hashed host inventory.
- `freebox_wifi_status`: bounded Wi-Fi access-point and station summaries.

Pass `includeHosts=true` only when host inventory is relevant. An optional `interfaceName` must be 1–64 characters
and contain no control characters.

## Live resource

`freebox://live/network-advanced` returns the sanitized network summary. If live data is unavailable, it returns
bounded fallback text rather than an exception or sensitive source details.

## Safety

- Do not expose IP addresses, public/private network names, SSIDs, BSSIDs, host names, domains, or raw MACs.
- Treat opaque hashed identifiers as identifiers, not as identities.
- This domain has no raw router configuration or firewall mutation operations.
