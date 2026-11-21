namespace Freenaute.Freebox.Mcp.Tools.Pairing;

public sealed record PairingInitiationToolResult(
    bool Initiated,
    string Status,
    string? Message);
