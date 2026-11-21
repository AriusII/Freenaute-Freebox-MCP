namespace Freenaute.Freebox.Mcp.Tools.Pairing;

public sealed record PairingStatusToolResult(
    bool CredentialStoreAvailable,
    string Status);
