namespace Freenaute.Freebox.Mcp.Tools.Shares;

public sealed record ShareInterfaceVisibilityToolResult(
    string Name,
    long? HostCount,
    int? ReachableHostCount,
    int? ActiveHostCount);
