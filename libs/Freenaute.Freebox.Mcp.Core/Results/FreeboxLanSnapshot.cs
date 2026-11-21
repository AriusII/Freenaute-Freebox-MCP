namespace Freenaute.Freebox.Mcp.Core.Results;

public sealed record FreeboxLanSnapshot(
    string? Name,
    string? Type,
    string? Mode,
    FreeboxLanInterfaceSnapshot[] Interfaces,
    IReadOnlyDictionary<string, FreeboxLanHostSnapshot[]> Hosts);
