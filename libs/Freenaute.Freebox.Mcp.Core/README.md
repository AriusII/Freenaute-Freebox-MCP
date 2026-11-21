# Freenaute.Freebox.Mcp.Core

Core library of the Freebox MCP server: configuration, orchestration, error mapping, credential-store abstraction,
and secret redaction. Targets `net11.0` / C# 15, `IsAotCompatible` + `IsTrimmable` (source-generated JSON only, no
reflection serialization).

## Role

- **`Configuration/FreeboxCoreOptions.cs`** — all tunables: `ServerAddress` (default `https://mafreebox.freebox.fr/`),
  `ApplicationId`, `ApplicationName`, `ApplicationVersion`, `DeviceName`, `CredentialProfile`, `RequestTimeout` (30 s),
  mDNS discovery (`EnableMdnsDiscovery`, `MdnsTimeout` 2 s), resilience (`EnableResilience`), and the
  `AllowPairingInitiation` policy switch. Validated on start (`ValidateOnStart`).
- **`DependencyInjection/FreeboxMcpCoreServiceCollectionExtensions.cs`** — `AddFreeboxMcpCore(...)` wires the
  `Freenaute.Freebox.Client` client, options, and `IFreeboxOrchestrator`; `AddFreeboxCredentialStorage<T>()` registers
  the platform storage + `FreeboxCredentialStoreAdapter`.
- **`Orchestration/FreeboxOrchestrator.cs`** — the single entry point used by the MCP tools: `DiscoverAsync`,
  `BeginPairingAsync` (gated by `AllowPairingInitiation`), `WaitForApprovalAsync` (cancellable; maps
  denied → `Denied`, timeout → `Expired`), `GetConnectionStatusAsync`, `GetNetworkSnapshotAsync`,
  `GetSystemSnapshotAsync`, `ForgetCredentialsAsync`. Credential ids are derived from the discovered Freebox UID +
  application id + profile.
- **`Orchestration/FreeboxCredentialStoreAdapter.cs`** — wraps `IFreeboxCredentialStorage`, enforces availability, and
  performs **write + read-back verification** with a fixed-time comparison.
- **`Orchestration/FreeboxErrorMapper.cs`** — maps exceptions to stable `FreeboxCoreErrorCode`s (`Cancelled`,
  `InvalidRequest`, `AuthenticationRequired`, `AuthorizationPending/Denied/Expired`, `ApiRejected`,
  `TransportFailure`, `CredentialStoreUnavailable`, `Unknown`) with redacted messages.
- **`Security/FreeboxSecretRedactor.cs`** — redacts `app_token`, `track_id`, `session_token`, `challenge`, `password`,
  `authorization`, `cookie` from any text (JSON / assignment forms, case-insensitive).
- **`Results/`** — immutable snapshot/result records (`FreeboxDiscoverySnapshot`, `FreeboxNetworkSnapshot`,
  `FreeboxSystemSnapshot`, `FreeboxCredentialState`, ...) consumed by the Tools layer.

## Dependencies

- NuGet: `Freenaute.Freebox.Client` **1.0.0** (Freebox OS HTTP client + pairing coordinator). Versions are centrally
  managed in `Directory.Packages.props` (CPM) with lock files.
- No dependency on the MCP SDK; this library is host-agnostic.

## AOT / trimming

`IsAotCompatible=true`, trim and single-file analyzers enabled repo-wide (`Directory.Build.props`). Serialization uses
`Serialization/FreeboxCoreJsonContext` (source-generated). Test projects opt out of the AOT analyzers
(`tests/Directory.Build.props`).
