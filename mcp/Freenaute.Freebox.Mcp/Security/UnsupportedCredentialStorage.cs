namespace Freenaute.Freebox.Mcp.Security;

internal sealed class UnsupportedCredentialStorage : IFreeboxCredentialStorage
{
    public bool IsAvailable => false;

    public string Kind => "unavailable";

    public ValueTask<FreeboxAppCredentials?> GetAsync(
        FreeboxCredentialId id,
        CancellationToken cancellationToken = default)
    {
        throw CreateException();
    }

    public ValueTask PutAsync(
        FreeboxCredentialId id,
        FreeboxAppCredentials credentials,
        CancellationToken cancellationToken = default)
    {
        throw CreateException();
    }

    public ValueTask DeleteAsync(
        FreeboxCredentialId id,
        CancellationToken cancellationToken = default)
    {
        throw CreateException();
    }

    private static NotSupportedException CreateException()
    {
        return new NotSupportedException(
            "Secure OS credential storage is unavailable on this platform. Refusing to use plaintext credential storage.");
    }
}
