namespace Freenaute.Freebox.Mcp.Core.Results;

public sealed record FreeboxDiscoverySnapshot(
    string? BoxModel,
    string? ApiVersion,
    bool HttpsAvailable,
    int? HttpsPort);
