#if HAS_LINUX_SECRET_SERVICE_STORAGE
namespace Freenaute.Freebox.Mcp.Core.Tests.Security;

[Collection(CredentialEnvironment.Name)]
public sealed class LinuxSecretServiceCredentialStorageTests
{
    [Fact]
    public void SecretToolAvailability_IsFalseWhenBackendBinaryIsAbsent()
    {
        if (LinuxSecretServiceCredentialStorage.IsSecretToolAvailable())
        {
            // A real libsecret backend is present; the fail-closed path is not reachable here.
            return;
        }

        Assert.False(LinuxSecretServiceCredentialStorage.IsSecretToolAvailable());
        var storage = new LinuxSecretServiceCredentialStorage();
        Assert.False(storage.IsAvailable);
        Assert.Equal("linux-secret-service-libsecret", storage.Kind);
    }

    [Fact]
    public async Task MissingSecretToolBackend_OperationsFailClosedWithoutPlaintextFallback()
    {
        if (LinuxSecretServiceCredentialStorage.IsSecretToolAvailable())
        {
            return;
        }

        var storage = new LinuxSecretServiceCredentialStorage();
        var id = new FreeboxCredentialId("default", "org.freenaute.freebox-mcp", "uid-1");
        var credentials = new FreeboxAppCredentials(
            "org.freenaute.freebox-mcp", "secret-token", FreeboxCredentialStatus.Approved, 1, DateTimeOffset.UtcNow);

        // The secret-tool process cannot start: every operation must throw instead of
        // silently succeeding or falling back to plaintext.
        await Assert.ThrowsAnyAsync<Exception>(async () =>
            await storage.GetAsync(id, TestContext.Current.CancellationToken));
        await Assert.ThrowsAnyAsync<Exception>(async () =>
            await storage.PutAsync(id, credentials, TestContext.Current.CancellationToken));
        await Assert.ThrowsAnyAsync<Exception>(async () =>
            await storage.DeleteAsync(id, TestContext.Current.CancellationToken));
    }

    [Fact]
    public void StoreKey_UsesOrgFreenauteServiceAttribute()
    {
        // The store key scopes the libsecret account attribute; it must carry the new app id prefix.
        var id = new FreeboxCredentialId("default", "org.freenaute.freebox-mcp", "uid-1");
        string key = CredentialStoreKey.For(id);

        Assert.StartsWith("org.freenaute.freebox-mcp/credentials:", key, StringComparison.Ordinal);
        Assert.DoesNotContain("io.github", key, StringComparison.Ordinal);
    }
}
#endif
