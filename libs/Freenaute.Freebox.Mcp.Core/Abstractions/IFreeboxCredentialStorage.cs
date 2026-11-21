namespace Freenaute.Freebox.Mcp.Core.Abstractions;

public interface IFreeboxCredentialStorage
{
    bool IsAvailable { get; }

    string Kind { get; }

    ValueTask<FreeboxAppCredentials?> GetAsync(
        FreeboxCredentialId id,
        CancellationToken cancellationToken = default);

    ValueTask PutAsync(
        FreeboxCredentialId id,
        FreeboxAppCredentials credentials,
        CancellationToken cancellationToken = default);

    ValueTask DeleteAsync(
        FreeboxCredentialId id,
        CancellationToken cancellationToken = default);
}
