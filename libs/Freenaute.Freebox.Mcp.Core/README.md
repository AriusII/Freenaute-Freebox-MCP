# Freenaute.Freebox.Mcp.Core

Core library of the Freebox MCP server: configuration, DI composition, repositories/services around the Freebox SDK,
orchestration, error mapping, credential-store abstraction, and secret redaction. Targets `net11.0` / C# 15,
`IsAotCompatible` + `IsTrimmable` (source-generated JSON only, no reflection serialization).

## Role

- **`Configuration/FreeboxCoreOptions.cs`** — all tunables: `ServerAddress` (default `https://mafreebox.freebox.fr/`),
  `ApplicationId`, `ApplicationName`, `ApplicationVersion`, `DeviceName`, `CredentialProfile`, `RequestTimeout` (30 s),
  mDNS discovery (`EnableMdnsDiscovery`, `MdnsTimeout` 2 s), resilience (`EnableResilience`), and the
  `AllowPairingInitiation` policy switch. Validated on start (`ValidateOnStart`).
- **`DependencyInjection/FreeboxMcpCoreServiceCollectionExtensions.cs`** — `AddFreeboxMcpCore(...)` wires the
  `Freenaute.Freebox.Client` client, options, repositories/services, and `IFreeboxOrchestrator`;
  `AddFreeboxCredentialStorage<T>()` registers the platform storage + `FreeboxCredentialStoreAdapter`;
  `AddFreeboxStartupAuthentication()` enables automatic startup authentication bootstrap.
- **`Repositories/`** — SDK-facing data/auth repositories (`IFreeboxDataRepository`,
  `IFreeboxAuthenticationRepository`) that wrap `Freenaute.Freebox.Client` APIs.
- **`Services/`** — startup/service-layer behaviors (`IFreeboxStartupAuthenticationService`,
  `FreeboxStartupAuthenticationHostedService`) to perform automatic credential reuse/pairing wait on server start.
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

- Runtime NuGet dependencies are centralized through `.Core` (including `Freenaute.Freebox.Client` and shared
  hosting/MCP runtime dependencies).
- Versions are centrally managed in `Directory.Packages.props` (CPM).

## AOT / trimming

`IsAotCompatible=true`, trim and single-file analyzers enabled repo-wide (`Directory.Build.props`). Serialization uses
`Serialization/FreeboxCoreJsonContext` (source-generated). Test projects opt out of the AOT analyzers
(`tests/Directory.Build.props`).
