namespace Freenaute.Freebox.Mcp.Tools.System;

public sealed record SystemToolResult(
    string? FirmwareVersion,
    string? Model,
    string? BoardName,
    long? UptimeSeconds,
    bool? BoxAuthenticated,
    bool? Usb3Enabled,
    bool? UserStoragePowered,
    string? DiskStatus,
    SystemSensorToolResult[] Sensors,
    SystemFanToolResult[] Fans,
    SystemExpansionToolResult[] Expansions);
