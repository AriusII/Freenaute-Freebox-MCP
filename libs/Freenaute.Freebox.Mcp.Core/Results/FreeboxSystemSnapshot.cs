namespace Freenaute.Freebox.Mcp.Core.Results;

public sealed record FreeboxSystemSnapshot(
    string? FirmwareVersion,
    string? Model,
    string? BoardName,
    long? UptimeSeconds,
    bool? BoxAuthenticated,
    bool? Usb3Enabled,
    bool? UserStoragePowered,
    string? DiskStatus,
    FreeboxSystemSensorSnapshot[] Sensors,
    FreeboxSystemSensorSnapshot[] Fans,
    FreeboxSystemExpansionSnapshot[] Expansions);
