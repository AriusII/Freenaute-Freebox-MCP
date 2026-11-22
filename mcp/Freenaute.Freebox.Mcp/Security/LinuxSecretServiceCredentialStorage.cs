namespace Freenaute.Freebox.Mcp.Security;

/// <summary>
///     Linux credential storage backed by libsecret through the <c>secret-tool</c> CLI. This is AOT-safe:
///     it only uses <see cref="Process" /> and never loads D-Bus bindings or uses reflection. Items are
///     stored with the attributes <c>service=org.freenaute.freebox-mcp</c> and
///     <c>account=&lt;SHA-256 store key&gt;</c>. Any backend failure fails closed with a
///     <see cref="NotSupportedException" />; there is never a plaintext fallback.
/// </summary>
internal sealed class LinuxSecretServiceCredentialStorage : IFreeboxCredentialStorage
{
    private const string SecretTool = "secret-tool";

    public bool IsAvailable => IsSecretToolAvailable();

    public string Kind => "linux-secret-service-libsecret";

    public async ValueTask<FreeboxAppCredentials?> GetAsync(
        FreeboxCredentialId id,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(id);
        cancellationToken.ThrowIfCancellationRequested();

        (int exitCode, byte[] stdout, _) = await RunSecretToolAsync(
            ["lookup", "service", BuildInfo.ApplicationId, "account", CredentialStoreKey.For(id)],
            null,
            cancellationToken).ConfigureAwait(false);

        if (exitCode == 1 && stdout.Length == 0)
        {
            return null;
        }

        if (exitCode != 0)
        {
            throw CreateBackendException("look up");
        }

        try
        {
            return CredentialPayloadCodec.Deserialize(stdout);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(stdout);
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
        try
        {
            (int exitCode, _, _) = await RunSecretToolAsync(
                [
                    "store", "--label=Freenaute Freebox MCP credential",
                    "service", BuildInfo.ApplicationId, "account", CredentialStoreKey.For(id)
                ],
                json,
                cancellationToken).ConfigureAwait(false);

            if (exitCode != 0)
            {
                throw CreateBackendException("store");
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(json);
        }
    }

    public async ValueTask DeleteAsync(
        FreeboxCredentialId id,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(id);
        cancellationToken.ThrowIfCancellationRequested();

        (int exitCode, _, _) = await RunSecretToolAsync(
            ["clear", "service", BuildInfo.ApplicationId, "account", CredentialStoreKey.For(id)],
            null,
            cancellationToken).ConfigureAwait(false);

        // secret-tool clear exits 1 when no matching item exists, which is the desired end state.
        if (exitCode is not 0 and not 1)
        {
            throw CreateBackendException("clear");
        }
    }

    public static bool IsSecretToolAvailable()
    {
        if (!OperatingSystem.IsLinux())
        {
            return false;
        }

        string? path = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrEmpty(path))
        {
            return false;
        }

        foreach (string directory in path.Split(Path.PathSeparator))
        {
            if (string.IsNullOrWhiteSpace(directory))
            {
                continue;
            }

            if (File.Exists(Path.Combine(directory.Trim(), SecretTool)))
            {
                return true;
            }
        }

        return false;
    }

    private static NotSupportedException CreateBackendException(string operation)
    {
        return new NotSupportedException(
            $"The libsecret backend (secret-tool) could not {operation} the Freebox credential; " +
            "failing closed instead of using plaintext storage.");
    }

    private static async Task<(int ExitCode, byte[] StandardOutput, string StandardError)> RunSecretToolAsync(
        IReadOnlyList<string> arguments,
        byte[]? stdinPayload,
        CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = SecretTool,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        Process process;
        try
        {
            process = Process.Start(startInfo) ?? throw CreateBackendException("start");
        }
        catch (Exception exception) when (exception is Win32Exception or FileNotFoundException)
        {
            throw CreateBackendException("start");
        }

        using (process)
        {
            Task<string> standardError = process.StandardError.ReadToEndAsync(cancellationToken);
            Task<byte[]> standardOutput = ReadAllBytesAsync(process.StandardOutput.BaseStream, cancellationToken);

            if (stdinPayload is not null)
            {
                await process.StandardInput.BaseStream.WriteAsync(stdinPayload, cancellationToken)
                    .ConfigureAwait(false);
            }

            process.StandardInput.Close();
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

            return (process.ExitCode, await standardOutput.ConfigureAwait(false),
                await standardError.ConfigureAwait(false));
        }
    }

    private static async Task<byte[]> ReadAllBytesAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
        return buffer.ToArray();
    }
}
