namespace Freenaute.Freebox.Mcp.Core.Services;

public interface IFreeboxStartupAuthenticationService
{
    ValueTask EnsureAuthenticatedAsync(CancellationToken cancellationToken = default);
}
