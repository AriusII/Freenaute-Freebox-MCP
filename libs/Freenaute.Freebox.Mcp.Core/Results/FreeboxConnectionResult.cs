namespace Freenaute.Freebox.Mcp.Core.Results;

public sealed record FreeboxConnectionResult(
    string CredentialStoreKind,
    bool CredentialStoreAvailable,
    FreeboxCredentialState CredentialState);
