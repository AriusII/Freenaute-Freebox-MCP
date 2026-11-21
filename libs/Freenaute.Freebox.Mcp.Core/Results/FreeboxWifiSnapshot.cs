namespace Freenaute.Freebox.Mcp.Core.Results;

public sealed record FreeboxWifiSnapshot(
    bool? Enabled,
    string? MacFilterState,
    string? State,
    FreeboxWifiAccessPointSnapshot[] AccessPoints,
    FreeboxWifiBssSnapshot[] Networks);
