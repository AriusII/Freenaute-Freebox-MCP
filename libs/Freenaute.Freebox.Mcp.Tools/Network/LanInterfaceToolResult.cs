namespace Freenaute.Freebox.Mcp.Tools.Network;

public sealed record LanInterfaceToolResult(string Name, long? HostCount, LanHostToolResult[]? Hosts);
