namespace Freenaute.Freebox.Mcp.Tools.AirMedia;

public sealed record AirMediaStatusToolResult(
    bool Enabled,
    AirMediaReceiverToolResult[] Receivers);
