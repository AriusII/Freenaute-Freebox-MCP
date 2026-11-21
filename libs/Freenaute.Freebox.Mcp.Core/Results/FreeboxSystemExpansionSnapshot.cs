namespace Freenaute.Freebox.Mcp.Core.Results;

public sealed record FreeboxSystemExpansionSnapshot(
    long? Slot,
    bool? Present,
    bool? Supported,
    string? Type);
