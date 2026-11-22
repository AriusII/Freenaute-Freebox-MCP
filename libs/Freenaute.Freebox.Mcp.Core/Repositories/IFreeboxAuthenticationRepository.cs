namespace Freenaute.Freebox.Mcp.Core.Repositories;

public interface IFreeboxAuthenticationRepository
{
    ValueTask<FreeboxApprovalRegistration> BeginPairingAsync(CancellationToken cancellationToken = default);

    ValueTask<FreeboxAppCredentials> WaitForApprovalAsync(
        TimeSpan? pollInterval = null,
        CancellationToken cancellationToken = default);

    ValueTask ForgetAsync(CancellationToken cancellationToken = default);
}
