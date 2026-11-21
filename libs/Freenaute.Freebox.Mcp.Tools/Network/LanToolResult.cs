namespace Freenaute.Freebox.Mcp.Tools.Network;

public sealed record LanToolResult(
    string? Name,
    string? Type,
    string? Mode,
    LanInterfaceToolResult[] Interfaces);
