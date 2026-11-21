namespace Freenaute.Freebox.Mcp.Core.Results;

public sealed record FreeboxCredentialResult(
    FreeboxCredentialState State,
    DateTimeOffset? CreatedAt = null,
    DateTimeOffset? UpdatedAt = null);
