namespace Freenaute.Freebox.Mcp.Tools.Shares;

public sealed record NasVisibilityToolResult(
    bool? UserStoragePowered,
    string? DiskStatus,
    string? FirmwareVersion,
    string? Model);
