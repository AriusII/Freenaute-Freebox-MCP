namespace Freenaute.Freebox.Mcp.Core.Results;

public sealed record FreeboxLanHostSnapshot(
    string? Id,
    string? HostType,
    bool? Reachable,
    bool? Active,
    long? LastActivity,
    string? Interface);
