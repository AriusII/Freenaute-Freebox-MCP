namespace Freenaute.Freebox.Mcp.Security;

internal sealed record CredentialPayload(
    int FormatVersion,
    string ApplicationId,
    string AppToken,
    FreeboxCredentialStatus Status,
    int? TrackId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
