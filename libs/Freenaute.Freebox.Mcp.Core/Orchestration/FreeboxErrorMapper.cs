namespace Freenaute.Freebox.Mcp.Core.Orchestration;

public static class FreeboxErrorMapper
{
    public static FreeboxCoreError Map(Exception exception, string? correlationId = null)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception switch
        {
            OperationCanceledException => new FreeboxCoreError(
                FreeboxCoreErrorCode.Cancelled,
                "The Freebox operation was cancelled.",
                CorrelationId: correlationId),

            FreeboxApiException api => new FreeboxCoreError(
                MapStatus(api),
                FreeboxSecretRedactor.Redact(api.ApiMessage ?? "The Freebox API rejected the request."),
                api.StatusCode is null ? null : (int)api.StatusCode.Value,
                api.ErrorCode,
                correlationId),

            FreeboxAuthorizationException authorization => new FreeboxCoreError(
                MapAuthorizationStatus(authorization.Status),
                "Freebox application authorization was not completed.",
                CorrelationId: correlationId),

            NotSupportedException => new FreeboxCoreError(
                FreeboxCoreErrorCode.CredentialStoreUnavailable,
                "Secure OS credential storage is unavailable on this platform.",
                CorrelationId: correlationId),

            FreeboxAuthenticationException => new FreeboxCoreError(
                FreeboxCoreErrorCode.AuthenticationRequired,
                "Freebox authentication is required. Pair or renew the application credentials.",
                CorrelationId: correlationId),

            HttpRequestException http => new FreeboxCoreError(
                FreeboxCoreErrorCode.TransportFailure,
                "The Freebox server could not be reached.",
                http.StatusCode is null ? null : (int)http.StatusCode.Value,
                CorrelationId: correlationId),

            ArgumentException => new FreeboxCoreError(
                FreeboxCoreErrorCode.InvalidRequest,
                FreeboxSecretRedactor.Redact(exception.Message),
                CorrelationId: correlationId),

            InvalidOperationException => new FreeboxCoreError(
                FreeboxCoreErrorCode.InvalidRequest,
                FreeboxSecretRedactor.Redact(exception.Message),
                CorrelationId: correlationId),

            _ => new FreeboxCoreError(
                FreeboxCoreErrorCode.Unknown,
                "The Freebox operation failed unexpectedly.",
                CorrelationId: correlationId)
        };
    }

    private static FreeboxCoreErrorCode MapStatus(FreeboxApiException exception)
    {
        return (int?)exception.StatusCode is 401 or 403
            ? FreeboxCoreErrorCode.AuthenticationRequired
            : FreeboxCoreErrorCode.ApiRejected;
    }

    private static FreeboxCoreErrorCode MapAuthorizationStatus(AuthorizationTrackEnum status)
    {
        return status switch
        {
            AuthorizationTrackEnum.Denied => FreeboxCoreErrorCode.AuthorizationDenied,
            AuthorizationTrackEnum.Timeout => FreeboxCoreErrorCode.AuthorizationExpired,
            _ => FreeboxCoreErrorCode.AuthorizationPending
        };
    }
}
