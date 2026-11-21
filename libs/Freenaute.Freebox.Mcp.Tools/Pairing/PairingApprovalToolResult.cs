namespace Freenaute.Freebox.Mcp.Tools.Pairing;

public sealed record PairingApprovalToolResult(
    bool Completed,
    string Status,
    string? Message);
