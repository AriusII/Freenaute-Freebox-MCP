# Freenaute.Freebox.Mcp.Tools

The 12 MCP tools exposed by the server, plus the sanitization layer and the result DTOs. Targets `net11.0` / C# 15,
`IsAotCompatible`, uses the .NET MCP SDK (`ModelContextProtocol` 2.4.0, `UsesMcpSdk=true`).

## Tools (`[McpServerToolType]` classes)

| Class                                    | Tool                         | `ReadOnly` | Notes                                                                                                                      |
|------------------------------------------|------------------------------|------------|----------------------------------------------------------------------------------------------------------------------------|
| `Discovery/FreeboxDiscoveryTools`        | `freebox_discover`           | yes        | Bounded, non-identifying API capability metadata (model, API version, HTTPS availability/port).                            |
| `Pairing/FreeboxPairingTools`            | `freebox_pairing_status`     | yes        | Credential store availability + state; never exposes secrets.                                                              |
| `Pairing/FreeboxPairingTools`            | `freebox_pairing_initiate`   | **no**     | Gated by `FREEBOX_ALLOW_PAIRING_INITIATION` (`1`/`true`/`TRUE`); physical approval on the Freebox display still required.  |
| `Pairing/FreeboxPairingTools`            | `freebox_pairing_wait`       | no         | Polls an existing pairing; cancellable/resumable via `CancellationToken`, with optional poll interval / timeout arguments. |
| `Network/FreeboxNetworkTools`            | `freebox_connection_status`  | yes        | Connection state/type/media, rates, byte counters, remote-access flags; no network addresses.                              |
| `Network/FreeboxNetworkTools`            | `freebox_lan_overview`       | yes        | Params: `includeHosts` (default `false`), `interfaceName` (1–64 chars, no control chars). Host ids are hashed.             |
| `Network/FreeboxNetworkTools`            | `freebox_wifi_status`        | yes        | Wi-Fi config/state, access points, BSS list; SSIDs/BSSIDs/host ids excluded or hashed.                                     |
| `Notifications/FreeboxNotificationTools` | `freebox_status_feed`        | yes        | Bounded sanitized status feed derived from pairing, connectivity, Wi-Fi, and system snapshots.                             |
| `Notifications/FreeboxNotificationTools` | `freebox_notifications_feed` | yes        | Action-oriented bounded notifications computed from sanitized status data.                                                 |
| `System/FreeboxSystemTools`              | `freebox_system_status`      | yes        | Firmware, model, board, uptime, authentication, disk, USB3, sensors, fans, expansions.                                     |
| `Parental/FreeboxParentalTools`          | `freebox_parental_config`    | yes        | Default parental policy mode only; no target identity data is exposed.                                                     |
| `Parental/FreeboxParentalTools`          | `freebox_parental_filters`   | yes        | Bounded sanitized filter view; MACs/hosts/IP/description are excluded, only counts and policy state remain.                |

Every tool declares `Destructive = false`, `OpenWorld = true`, `UseStructuredContent = true` and a typed
`OutputSchemaType`; failures are returned as structured errors via `Security/ToolResultFactory` (mapped
`FreeboxCoreError`, redacted message, correlation id).

## Sanitization (`Security/Safe.cs`)

- `Safe.Text(value, max)` — strips control characters and truncates to `max` (32–64 depending on the field).
- `Safe.StableIdentifier(id)` — SHA-256 hex digest replacing MAC/host/BSS/sensor identifiers.

## Output bounds

Hard caps applied before returning: 32 LAN interfaces, 128 hosts per interface, 32 Wi-Fi access points, 128 BSS,
32 system components (sensors/fans/expansions). Result JSON uses the source-generated
`Serialization/ToolResultJsonSerializerContext` (AOT-safe).

## Dependencies

- Project: `Freenaute.Freebox.Mcp.Core` (orchestrator + result types).
- NuGet: `ModelContextProtocol` (CPM, `Directory.Packages.props`).
