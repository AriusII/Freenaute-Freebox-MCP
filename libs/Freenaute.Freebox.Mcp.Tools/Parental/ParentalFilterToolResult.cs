namespace Freenaute.Freebox.Mcp.Tools.Parental;

public sealed record ParentalFilterToolResult(
    string StableId,
    int MacAddressCount,
    int HostCount,
    bool HasIpAddress,
    bool HasDescription,
    bool? Forced,
    string ForcedMode,
    long? TemporaryModeExpiresAt,
    string TemporaryMode,
    string SchedulingMode,
    string FilterState,
    long? CurrentMappingIndex,
    long? NextChange);
