namespace Freenaute.Freebox.Mcp.Core.Orchestration;

public sealed class FreeboxCredentialStoreAdapter(IFreeboxCredentialStorage storage) : IFreeboxCredentialStore
{
    private readonly IFreeboxCredentialStorage _storage = storage ?? throw new ArgumentNullException(nameof(storage));

    public bool IsAvailable => _storage.IsAvailable;

    public string Kind => _storage.Kind;

    public ValueTask<FreeboxAppCredentials?> GetAsync(
        FreeboxCredentialId id,
        CancellationToken cancellationToken = default)
    {
        EnsureAvailable();
        return _storage.GetAsync(id, cancellationToken);
    }

    public async ValueTask PutAsync(
        FreeboxCredentialId id,
        FreeboxAppCredentials credentials,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(credentials);
        EnsureAvailable();
        await _storage.PutAsync(id, credentials, cancellationToken).ConfigureAwait(false);
        FreeboxAppCredentials? stored = await _storage.GetAsync(id, cancellationToken).ConfigureAwait(false);
        if (!Equivalent(credentials, stored))
        {
            throw new InvalidOperationException("Secure credential storage verification failed after write.");
        }
    }

    public async ValueTask DeleteAsync(
        FreeboxCredentialId id,
        CancellationToken cancellationToken = default)
    {
        EnsureAvailable();
        await _storage.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
        if (await _storage.GetAsync(id, cancellationToken).ConfigureAwait(false) is not null)
        {
            throw new InvalidOperationException("Secure credential storage verification failed after delete.");
        }
    }

    private static bool Equivalent(FreeboxAppCredentials expected, FreeboxAppCredentials? actual)
    {
        return actual is not null &&
               string.Equals(actual.ApplicationId, expected.ApplicationId, StringComparison.Ordinal) &&
               actual.Status == expected.Status &&
               actual.TrackId == expected.TrackId &&
               actual.CreatedAt == expected.CreatedAt &&
               FixedTimeEquals(actual.GetAppToken(), expected.GetAppToken());
    }

    private void EnsureAvailable()
    {
        if (!_storage.IsAvailable)
        {
            throw new NotSupportedException(
                $"Secure credential storage '{_storage.Kind}' is unavailable on this platform.");
        }
    }

    private static bool FixedTimeEquals(string left, string right)
    {
        byte[] leftBytes = Encoding.UTF8.GetBytes(left);
        byte[] rightBytes = Encoding.UTF8.GetBytes(right);
        try
        {
            return leftBytes.Length == rightBytes.Length &&
                   CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(leftBytes);
            CryptographicOperations.ZeroMemory(rightBytes);
        }
    }
}
