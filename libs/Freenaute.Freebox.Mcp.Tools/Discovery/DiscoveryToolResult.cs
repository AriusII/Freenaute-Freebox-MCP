namespace Freenaute.Freebox.Mcp.Tools.Discovery;

public sealed record DiscoveryToolResult(
    string BoxModel,
    string ApiVersion,
    bool HttpsAvailable,
    int? HttpsPort);
