# Freenaute.Freebox.Mcp.Tools

`Freenaute.Freebox.Mcp.Tools` contains the MCP tool surface for the Freebox server.

- Target: `net11.0`
- AOT posture: compatible (source-generated serialization, bounded DTOs)
- Depends on: [Freenaute.Freebox.Mcp.Core](../Freenaute.Freebox.Mcp.Core/README.md)

## Tool surface

- **12 MCP tool classes**
- **31 MCP tools**

### Tool classes

- `Discovery/FreeboxDiscoveryTools`
- `Pairing/FreeboxPairingTools`
- `Network/FreeboxNetworkTools`
- `System/FreeboxSystemTools`
- `Downloads/FreeboxDownloadsTools`
- `Calls/FreeboxCallTools`
- `Contacts/FreeboxContactsTools`
- `Cameras/FreeboxCamerasTools`
- `Notifications/FreeboxNotificationTools`
- `AirMedia/FreeboxAirMediaTools`
- `Shares/FreeboxSharesTools`
- `Parental/FreeboxParentalTools`

### Canonical tool names (31)

`freebox_airmedia_receivers`, `freebox_airmedia_status`, `freebox_calls_account`, `freebox_calls_clear_log`,
`freebox_calls_log`, `freebox_calls_mark_all_read`, `freebox_cameras_overview`, `freebox_connection_status`,
`freebox_contacts_list`, `freebox_contacts_search`, `freebox_discover`, `freebox_download_configuration`,
`freebox_download_files`, `freebox_download_log`, `freebox_download_pieces`, `freebox_download_stats`,
`freebox_download_status`, `freebox_downloads_list`, `freebox_home_status`, `freebox_lan_overview`,
`freebox_nas_visibility`, `freebox_notifications_feed`, `freebox_pairing_initiate`, `freebox_pairing_status`,
`freebox_pairing_wait`, `freebox_parental_config`, `freebox_parental_filters`, `freebox_shares_visibility`,
`freebox_status_feed`, `freebox_system_status`, `freebox_wifi_status`

## Output contracts and safety

- Structured output only (`UseStructuredContent = true`)
- Sanitized and bounded data
- Stable identifier hashing where required
- Typed error mapping from Core (`FreeboxCoreErrorCode`)

Serialization context:

- [ToolResultJsonSerializerContext](./Serialization/ToolResultJsonSerializerContext.cs)

## Notes

- Pairing initiation is policy-gated (`FREEBOX_ALLOW_PAIRING_INITIATION`)
- Pairing wait supports polling/timeout overrides with strict validation
- Most tools are read-only; explicit mutation tools are limited and intentionally narrow
