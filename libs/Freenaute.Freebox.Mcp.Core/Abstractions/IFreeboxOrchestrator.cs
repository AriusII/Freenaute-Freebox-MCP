namespace Freenaute.Freebox.Mcp.Core.Abstractions;

public interface IFreeboxOrchestrator
{
    ValueTask<FreeboxCoreResult<FreeboxPairingResult>> BeginPairingAsync(CancellationToken cancellationToken = default);

    async ValueTask<FreeboxCoreResult<FreeboxPairingResult>> BeginStartupPairingAsync(
        CancellationToken cancellationToken = default)
    {
        return await BeginPairingAsync(cancellationToken).ConfigureAwait(false);
    }

    ValueTask<FreeboxCoreResult<FreeboxCredentialResult>> WaitForApprovalAsync(
        TimeSpan? pollInterval = null,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default);

    ValueTask<FreeboxCoreResult<FreeboxConnectionResult>> GetConnectionStatusAsync(
        CancellationToken cancellationToken = default);

    ValueTask<FreeboxCoreResult<bool>> ForgetCredentialsAsync(CancellationToken cancellationToken = default);

    ValueTask<FreeboxCoreResult<FreeboxDiscoverySnapshot>> DiscoverAsync(
        CancellationToken cancellationToken = default);

    ValueTask<FreeboxCoreResult<FreeboxNetworkSnapshot>> GetNetworkSnapshotAsync(
        bool includeHosts = false,
        string? interfaceName = null,
        CancellationToken cancellationToken = default);

    ValueTask<FreeboxCoreResult<FreeboxSystemSnapshot>> GetSystemSnapshotAsync(
        CancellationToken cancellationToken = default);
}
