namespace Freenaute.Freebox.Mcp.Tools.Network;

public sealed record WifiToolResult(
    bool? Enabled,
    string? MacFilterState,
    string? State,
    WifiAccessPointToolResult[] AccessPoints,
    WifiBssToolResult[] Networks);
