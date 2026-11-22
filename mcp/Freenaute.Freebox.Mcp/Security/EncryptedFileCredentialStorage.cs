namespace Freenaute.Freebox.Mcp.Security;

/// <summary>
///     AES-256-GCM encrypted credential file storage for container/headless deployments, selected when
///     <c>FREEBOX_CREDENTIAL_FILE</c> is set. The encryption key is derived with PBKDF2-SHA256 from the
///     passphrase read from <c>FREEBOX_CREDENTIAL_PASSPHRASE_FILE</c> and a per-file random salt.
///     The stored blob is <c>FBMCP2 || salt(16) || nonce(12) || tag(16) || ciphertext</c>.
///     Legacy <c>FBMCP1</c> files (without salt) can still be decrypted for migration.
///     Writes are verified by reading the file back and comparing with a constant-time comparison.
/// </summary>
internal sealed class EncryptedFileCredentialStorage : IFreeboxCredentialStorage, IDisposable
{
    private const int AesKeySizeBytes = 32;
    private const int NonceSizeBytes = 12;
    private const int TagSizeBytes = 16;
    private const int SaltSizeBytes = 16;
    private const int Pbkdf2Iterations = 210_000;

    private readonly string _filePath;
    private readonly byte[] _legacyKey;
    private readonly byte[] _passphrase;

    public EncryptedFileCredentialStorage()
    {
        string? filePath = Environment.GetEnvironmentVariable("FREEBOX_CREDENTIAL_FILE");
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new InvalidOperationException(
                "Encrypted file credential storage requires FREEBOX_CREDENTIAL_FILE to be set.");
        }

        string? passphraseFile = Environment.GetEnvironmentVariable("FREEBOX_CREDENTIAL_PASSPHRASE_FILE");
        if (string.IsNullOrWhiteSpace(passphraseFile))
        {
            throw new InvalidOperationException(
                "FREEBOX_CREDENTIAL_PASSPHRASE_FILE must point to the passphrase file " +
                "when FREEBOX_CREDENTIAL_FILE is set.");
        }

        _filePath = Path.GetFullPath(filePath);

        byte[] passphrase = File.ReadAllBytes(passphraseFile);
        try
        {
            int length = passphrase.Length;
            while (length > 0 && (passphrase[length - 1] == (byte)'\n' || passphrase[length - 1] == (byte)'\r'))
            {
                length--;
            }

            if (length == 0)
            {
                throw new InvalidOperationException("The passphrase file is empty.");
            }

            _passphrase = passphrase.AsSpan(0, length).ToArray();
            _legacyKey = SHA256.HashData(_passphrase);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(passphrase);
        }
    }

    private static ReadOnlySpan<byte> MagicV1 => "FBMCP1"u8;
    private static ReadOnlySpan<byte> MagicV2 => "FBMCP2"u8;

    public void Dispose()
    {
        CryptographicOperations.ZeroMemory(_passphrase);
        CryptographicOperations.ZeroMemory(_legacyKey);
        GC.SuppressFinalize(this);
    }

    public bool IsAvailable => true;

    public string Kind => "encrypted-file-aes-256-gcm";

    public ValueTask<FreeboxAppCredentials?> GetAsync(
        FreeboxCredentialId id,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(id);
        cancellationToken.ThrowIfCancellationRequested();

        if (!File.Exists(_filePath))
        {
            return ValueTask.FromResult<FreeboxAppCredentials?>(null);
        }

        byte[] fileBytes = File.ReadAllBytes(_filePath);
        try
        {
            byte[] json = Decrypt(fileBytes);
            try
            {
                return ValueTask.FromResult(CredentialPayloadCodec.Deserialize(json));
            }
            finally
            {
                CryptographicOperations.ZeroMemory(json);
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(fileBytes);
        }
    }

    public async ValueTask PutAsync(
        FreeboxCredentialId id,
        FreeboxAppCredentials credentials,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(credentials);
        cancellationToken.ThrowIfCancellationRequested();

        byte[] json = CredentialPayloadCodec.Serialize(credentials);
        byte[] blob;
        try
        {
            blob = Encrypt(json);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(json);
        }

        try
        {
            string? directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllBytesAsync(_filePath, blob, cancellationToken).ConfigureAwait(false);

            byte[] readBack = await File.ReadAllBytesAsync(_filePath, cancellationToken).ConfigureAwait(false);
            try
            {
                if (!CryptographicOperations.FixedTimeEquals(readBack, blob))
                {
                    throw new InvalidDataException(
                        "The encrypted credential file could not be verified after the write.");
                }
            }
            finally
            {
                CryptographicOperations.ZeroMemory(readBack);
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(blob);
        }
    }

    public ValueTask DeleteAsync(
        FreeboxCredentialId id,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(id);
        cancellationToken.ThrowIfCancellationRequested();

        if (File.Exists(_filePath))
        {
            File.Delete(_filePath);
        }

        return ValueTask.CompletedTask;
    }

    private byte[] Encrypt(byte[] plaintext)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSizeBytes);
        byte[] nonce = RandomNumberGenerator.GetBytes(NonceSizeBytes);
        byte[] tag = new byte[TagSizeBytes];
        byte[] ciphertext = new byte[plaintext.Length];
        byte[] key = DeriveKey(salt);
        try
        {
            using var aes = new AesGcm(key, TagSizeBytes);
            aes.Encrypt(nonce, plaintext, ciphertext, tag);

            int offset = 0;
            byte[] blob = new byte[MagicV2.Length + SaltSizeBytes + NonceSizeBytes + TagSizeBytes + ciphertext.Length];
            MagicV2.CopyTo(blob);
            offset += MagicV2.Length;
            salt.CopyTo(blob, offset);
            offset += SaltSizeBytes;
            nonce.CopyTo(blob, offset);
            offset += NonceSizeBytes;
            tag.CopyTo(blob, offset);
            offset += TagSizeBytes;
            ciphertext.CopyTo(blob, offset);
            return blob;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
            CryptographicOperations.ZeroMemory(salt);
            CryptographicOperations.ZeroMemory(nonce);
            CryptographicOperations.ZeroMemory(tag);
            CryptographicOperations.ZeroMemory(ciphertext);
        }
    }

    private byte[] Decrypt(byte[] blob)
    {
        if (blob.AsSpan(0, Math.Min(MagicV2.Length, blob.Length)).SequenceEqual(MagicV2))
        {
            return DecryptV2(blob);
        }

        if (blob.AsSpan(0, Math.Min(MagicV1.Length, blob.Length)).SequenceEqual(MagicV1))
        {
            return DecryptV1(blob);
        }

        throw new InvalidDataException("The encrypted credential file is missing or has an invalid header.");
    }

    private byte[] DecryptV2(byte[] blob)
    {
        int headerSize = MagicV2.Length + SaltSizeBytes + NonceSizeBytes + TagSizeBytes;
        if (blob.Length <= headerSize)
        {
            throw new InvalidDataException("The encrypted credential file is missing or has an invalid header.");
        }

        byte[] key = DeriveKey(blob.AsSpan(MagicV2.Length, SaltSizeBytes));
        byte[] plaintext = new byte[blob.Length - headerSize];
        try
        {
            int nonceOffset = MagicV2.Length + SaltSizeBytes;
            int tagOffset = nonceOffset + NonceSizeBytes;
            using var aes = new AesGcm(key, TagSizeBytes);
            aes.Decrypt(
                blob.AsSpan(nonceOffset, NonceSizeBytes),
                blob.AsSpan(headerSize),
                blob.AsSpan(tagOffset, TagSizeBytes),
                plaintext);
            return plaintext;
        }
        catch (CryptographicException exception)
        {
            CryptographicOperations.ZeroMemory(plaintext);
            throw new InvalidDataException(
                "The encrypted credential file could not be decrypted (wrong passphrase or tampered file).",
                exception);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
        }
    }

    private byte[] DecryptV1(byte[] blob)
    {
        int headerSize = MagicV1.Length + NonceSizeBytes + TagSizeBytes;
        if (blob.Length <= headerSize)
        {
            throw new InvalidDataException("The encrypted credential file is missing or has an invalid header.");
        }

        byte[] plaintext = new byte[blob.Length - headerSize];
        try
        {
            using var aes = new AesGcm(_legacyKey, TagSizeBytes);
            aes.Decrypt(
                blob.AsSpan(MagicV1.Length, NonceSizeBytes),
                blob.AsSpan(headerSize),
                blob.AsSpan(MagicV1.Length + NonceSizeBytes, TagSizeBytes),
                plaintext);
            return plaintext;
        }
        catch (CryptographicException exception)
        {
            CryptographicOperations.ZeroMemory(plaintext);
            throw new InvalidDataException(
                "The encrypted credential file could not be decrypted (wrong passphrase or tampered file).",
                exception);
        }
    }

    private byte[] DeriveKey(ReadOnlySpan<byte> salt)
    {
        byte[] key = new byte[AesKeySizeBytes];
        Rfc2898DeriveBytes.Pbkdf2(_passphrase, salt, key, Pbkdf2Iterations, HashAlgorithmName.SHA256);
        return key;
    }
}
