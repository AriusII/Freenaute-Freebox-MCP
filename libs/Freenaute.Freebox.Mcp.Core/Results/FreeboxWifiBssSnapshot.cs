namespace Freenaute.Freebox.Mcp.Core.Results;

public sealed record FreeboxWifiBssSnapshot(
    string? Id,
    long? PhyId,
    string? State,
    long? StationCount,
    long? AuthorizedStationCount,
    bool? Enabled,
    string? Encryption,
    bool? WpsEnabled);
