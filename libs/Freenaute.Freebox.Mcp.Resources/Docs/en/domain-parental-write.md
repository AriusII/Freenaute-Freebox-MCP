# Parental controls write domain

Parental tools can expose configuration and filter summaries, but the current Freebox MCP write surface does not
provide policy mutation. Never imply that a filter, schedule, or device policy can be changed when only
`freebox_parental_config` or `freebox_parental_filters` is available.

## Current tools

- `freebox_parental_config`: sanitized default filter mode.
- `freebox_parental_filters`: at most 128 bounded filter summaries.

Identifiers are opaque hashes. MAC addresses, host names, IP addresses, descriptions, and raw targets are excluded.
`freebox://live/parental-write` is a live resource summary, not a mutation command.
