namespace Freenaute.Freebox.Mcp.Tools.Network;

public sealed record WifiBssToolResult(
    string? StableId,
    long? PhysicalId,
    string? State,
    long? StationCount,
    long? AuthorizedStationCount,
    bool? Enabled,
    string? Encryption,
    bool? WpsEnabled);
