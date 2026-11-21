namespace Freenaute.Freebox.Mcp.Tools.Network;

public sealed record LanHostToolResult(
    string StableId,
    string? DisplayName,
    string? HostType,
    string? Vendor,
    bool? Reachable,
    bool? Active,
    long? LastActivity,
    string? Interface);
