namespace Freenaute.Freebox.Mcp.Core.Results;

public sealed record FreeboxNetworkSnapshot(
    FreeboxConnectionSnapshot Connection,
    FreeboxLanSnapshot Lan,
    FreeboxWifiSnapshot Wifi);
