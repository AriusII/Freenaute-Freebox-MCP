namespace Freenaute.Freebox.Mcp.Tools.Network;

public sealed record ConnectionToolResult(
    string? State,
    string? Type,
    string? Media,
    long? RateUp,
    long? RateDown,
    long? BandwidthUp,
    long? BandwidthDown,
    long? BytesUp,
    long? BytesDown,
    bool? PingEnabled,
    bool? ApiRemoteAccessEnabled,
    bool? RemoteAccessEnabled,
    bool? WakeOnLanEnabled,
    string? SipAlg);
