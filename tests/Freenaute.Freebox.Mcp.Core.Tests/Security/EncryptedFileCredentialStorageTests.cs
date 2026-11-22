#if HAS_ENCRYPTED_FILE_CREDENTIAL_STORAGE
namespace Freenaute.Freebox.Mcp.Core.Tests.Security;

[Collection(CredentialEnvironment.Name)]
public sealed class EncryptedFileCredentialStorageTests : IDisposable
{
    private readonly string _credentialFile;

    private readonly string _directory =
        Path.Combine(Path.GetTempPath(), "freenaute-encrypted-storage-" + Guid.NewGuid().ToString("N"));

    private readonly string _passphraseFile;
    private readonly string? _previousCredentialFile;
    private readonly string? _previousPassphraseFile;

    public EncryptedFileCredentialStorageTests()
    {
        Directory.CreateDirectory(_directory);
        _credentialFile = Path.Combine(_directory, "credentials.bin");
        _passphraseFile = Path.Combine(_directory, "passphrase.txt");
        File.WriteAllText(_passphraseFile, "correct horse battery staple\n");
        _previousCredentialFile = Environment.GetEnvironmentVariable("FREEBOX_CREDENTIAL_FILE");
        _previousPassphraseFile = Environment.GetEnvironmentVariable("FREEBOX_CREDENTIAL_PASSPHRASE_FILE");
        Environment.SetEnvironmentVariable("FREEBOX_CREDENTIAL_FILE", _credentialFile);
        Environment.SetEnvironmentVariable("FREEBOX_CREDENTIAL_PASSPHRASE_FILE", _passphraseFile);
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable("FREEBOX_CREDENTIAL_FILE", _previousCredentialFile);
        Environment.SetEnvironmentVariable("FREEBOX_CREDENTIAL_PASSPHRASE_FILE", _previousPassphraseFile);
        try
        {
            Directory.Delete(_directory, true);
        }
        catch (IOException)
        {
            // Best effort cleanup of the temporary credential directory.
        }
    }

    [Fact]
    public async Task PutThenGet_RoundTripsCredentialThroughEncryptedFile_AndDeleteRemovesIt()
    {
        using var storage = new EncryptedFileCredentialStorage();
        var id = new FreeboxCredentialId("default", "org.freenaute.freebox-mcp", "uid-1");
        FreeboxAppCredentials credentials = CreateCredentials();

        Assert.True(storage.IsAvailable);
        Assert.Equal("encrypted-file-aes-256-gcm", storage.Kind);

        await storage.PutAsync(id, credentials, TestContext.Current.CancellationToken);
        FreeboxAppCredentials? restored = await storage.GetAsync(id, TestContext.Current.CancellationToken);

        Assert.NotNull(restored);
        Assert.Equal(credentials.ApplicationId, restored.ApplicationId);
        Assert.Equal(credentials.GetAppToken(), restored.GetAppToken());
        Assert.Equal(credentials.Status, restored.Status);
        Assert.Equal(credentials.TrackId, restored.TrackId);
        Assert.Equal(credentials.CreatedAt, restored.CreatedAt);

        await storage.DeleteAsync(id, TestContext.Current.CancellationToken);
        Assert.Null(await storage.GetAsync(id, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetAsync_MissingFile_ReturnsNull()
    {
        using var storage = new EncryptedFileCredentialStorage();
        var id = new FreeboxCredentialId("default", "org.freenaute.freebox-mcp", "uid-1");

        Assert.Null(await storage.GetAsync(id, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task StoredFile_HasMagicHeaderAndDoesNotContainPlaintextToken()
    {
        using var storage = new EncryptedFileCredentialStorage();
        var id = new FreeboxCredentialId("default", "org.freenaute.freebox-mcp", "uid-1");

        await storage.PutAsync(id, CreateCredentials("super-secret-token-value"),
            TestContext.Current.CancellationToken);

        byte[] raw = await File.ReadAllBytesAsync(_credentialFile, TestContext.Current.CancellationToken);
        Assert.True(raw.AsSpan(0, 6).SequenceEqual("FBMCP2"u8));
        Assert.DoesNotContain("super-secret-token-value", Encoding.UTF8.GetString(raw), StringComparison.Ordinal);
        Assert.DoesNotContain("super-secret-token-value", Convert.ToHexString(raw), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RewritingSameCredential_UsesDifferentCiphertext()
    {
        using var storage = new EncryptedFileCredentialStorage();
        var id = new FreeboxCredentialId("default", "org.freenaute.freebox-mcp", "uid-1");
        FreeboxAppCredentials credentials = CreateCredentials("same-token");

        await storage.PutAsync(id, credentials, TestContext.Current.CancellationToken);
        byte[] first = await File.ReadAllBytesAsync(_credentialFile, TestContext.Current.CancellationToken);
        await storage.PutAsync(id, credentials, TestContext.Current.CancellationToken);
        byte[] second = await File.ReadAllBytesAsync(_credentialFile, TestContext.Current.CancellationToken);

        Assert.False(CryptographicOperations.FixedTimeEquals(first, second));
    }

    [Fact]
    public async Task WrongPassphrase_FailsClosedWithoutLeakingToken()
    {
        using var storage = new EncryptedFileCredentialStorage();
        var id = new FreeboxCredentialId("default", "org.freenaute.freebox-mcp", "uid-1");
        await storage.PutAsync(id, CreateCredentials("wrong-pass-secret"), TestContext.Current.CancellationToken);

        File.WriteAllText(_passphraseFile, "a-completely-different-passphrase");
        using var wrongPassphraseStorage = new EncryptedFileCredentialStorage();

        InvalidDataException exception = await Assert.ThrowsAsync<InvalidDataException>(async () =>
            await wrongPassphraseStorage.GetAsync(id, TestContext.Current.CancellationToken));

        Assert.DoesNotContain("wrong-pass-secret", exception.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task LegacyV1Blob_IsStillReadableForMigration()
    {
        var id = new FreeboxCredentialId("default", "org.freenaute.freebox-mcp", "uid-1");
        FreeboxAppCredentials expected = CreateCredentials("legacy-token");
        byte[] payload = CredentialPayloadCodec.Serialize(expected);
        byte[] passphrase = Encoding.UTF8.GetBytes("correct horse battery staple");
        byte[] key = SHA256.HashData(passphrase);
        byte[] nonce = RandomNumberGenerator.GetBytes(12);
        byte[] tag = new byte[16];
        byte[] ciphertext = new byte[payload.Length];
        try
        {
            using var aes = new AesGcm(key, 16);
            aes.Encrypt(nonce, payload, ciphertext, tag);
            byte[] blob = new byte[6 + nonce.Length + tag.Length + ciphertext.Length];
            "FBMCP1"u8.CopyTo(blob);
            nonce.CopyTo(blob, 6);
            tag.CopyTo(blob, 6 + nonce.Length);
            ciphertext.CopyTo(blob, 6 + nonce.Length + tag.Length);
            await File.WriteAllBytesAsync(_credentialFile, blob, TestContext.Current.CancellationToken);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(payload);
            CryptographicOperations.ZeroMemory(passphrase);
            CryptographicOperations.ZeroMemory(key);
            CryptographicOperations.ZeroMemory(nonce);
            CryptographicOperations.ZeroMemory(tag);
            CryptographicOperations.ZeroMemory(ciphertext);
        }

        using var storage = new EncryptedFileCredentialStorage();
        FreeboxAppCredentials? restored = await storage.GetAsync(id, TestContext.Current.CancellationToken);

        Assert.NotNull(restored);
        Assert.Equal("legacy-token", restored.GetAppToken());
    }

    [Fact]
    public async Task TamperedFile_FailsClosedWithoutLeakingToken()
    {
        using var storage = new EncryptedFileCredentialStorage();
        var id = new FreeboxCredentialId("default", "org.freenaute.freebox-mcp", "uid-1");
        await storage.PutAsync(id, CreateCredentials("tamper-secret"), TestContext.Current.CancellationToken);

        byte[] raw = await File.ReadAllBytesAsync(_credentialFile, TestContext.Current.CancellationToken);
        raw[^1] ^= 0xFF;
        await File.WriteAllBytesAsync(_credentialFile, raw, TestContext.Current.CancellationToken);

        InvalidDataException exception = await Assert.ThrowsAsync<InvalidDataException>(async () =>
            await storage.GetAsync(id, TestContext.Current.CancellationToken));

        Assert.DoesNotContain("tamper-secret", exception.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void MissingCredentialFileEnv_ConstructorThrowsInsteadOfUsingPlaintext()
    {
        Environment.SetEnvironmentVariable("FREEBOX_CREDENTIAL_FILE", null);

        InvalidOperationException exception =
            Assert.Throws<InvalidOperationException>(() => new EncryptedFileCredentialStorage());

        Assert.Contains("FREEBOX_CREDENTIAL_FILE", exception.Message, StringComparison.Ordinal);
    }

    private static FreeboxAppCredentials CreateCredentials(string token = "secret-app-token")
    {
        return new FreeboxAppCredentials(
            "org.freenaute.freebox-mcp",
            token,
            FreeboxCredentialStatus.Approved,
            42,
            DateTimeOffset.Parse("2026-01-02T03:04:05Z", CultureInfo.InvariantCulture));
    }
}
#endif
