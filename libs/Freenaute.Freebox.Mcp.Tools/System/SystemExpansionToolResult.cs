namespace Freenaute.Freebox.Mcp.Tools.System;

public sealed record SystemExpansionToolResult(
    long? Slot,
    bool? Present,
    bool? Supported,
    string? Bundle,
    string? Type);
