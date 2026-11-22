namespace Freenaute.Freebox.Mcp.Tools.Shares;

public sealed record SharesVisibilityToolResult(
    string? LanName,
    string? LanType,
    string? LanMode,
    int InterfaceCount,
    int? ReachableHostCount,
    int? ActiveHostCount,
    ShareInterfaceVisibilityToolResult[] Interfaces);
