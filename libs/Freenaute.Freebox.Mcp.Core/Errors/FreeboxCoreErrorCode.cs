namespace Freenaute.Freebox.Mcp.Core.Errors;

public enum FreeboxCoreErrorCode
{
    Unknown,
    Cancelled,
    InvalidRequest,
    AuthenticationRequired,
    AuthorizationPending,
    AuthorizationDenied,
    AuthorizationExpired,
    ApiRejected,
    TransportFailure,
    CredentialStoreUnavailable
}
