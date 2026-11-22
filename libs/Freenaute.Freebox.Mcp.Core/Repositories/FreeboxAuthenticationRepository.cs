namespace Freenaute.Freebox.Mcp.Core.Repositories;

public sealed class FreeboxAuthenticationRepository(IFreeboxAuthorizationCoordinator authorizationCoordinator)
    : IFreeboxAuthenticationRepository
{
    private readonly IFreeboxAuthorizationCoordinator _authorizationCoordinator =
        authorizationCoordinator ?? throw new ArgumentNullException(nameof(authorizationCoordinator));

    public ValueTask<FreeboxApprovalRegistration> BeginPairingAsync(CancellationToken cancellationToken = default)
    {
        return _authorizationCoordinator.BeginPairingAsync(cancellationToken);
    }

    public ValueTask<FreeboxAppCredentials> WaitForApprovalAsync(
        TimeSpan? pollInterval = null,
        CancellationToken cancellationToken = default)
    {
        return _authorizationCoordinator.WaitForApprovalAsync(pollInterval, cancellationToken);
    }

    public ValueTask ForgetAsync(CancellationToken cancellationToken = default)
    {
        return _authorizationCoordinator.ForgetAsync(cancellationToken);
    }
}
