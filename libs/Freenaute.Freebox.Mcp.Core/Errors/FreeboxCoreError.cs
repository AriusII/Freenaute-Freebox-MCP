namespace Freenaute.Freebox.Mcp.Core.Errors;

public sealed record FreeboxCoreError(
    FreeboxCoreErrorCode Code,
    string Message,
    int? HttpStatusCode = null,
    string? ApiErrorCode = null,
    string? CorrelationId = null);
