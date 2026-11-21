namespace Freenaute.Freebox.Mcp.Security;

internal static class CredentialStorageRegistration
{
    /// <summary>
    ///     Registers the platform secure credential storage. Selection order (first match wins):
    ///     1. <c>FREEBOX_CREDENTIAL_FILE</c> set → AES-256-GCM encrypted file (container/headless mode).
    ///     2. Windows → Credential Manager via advapi32.
    ///     3. Linux with <c>secret-tool</c> on PATH → libsecret through the secret-tool CLI.
    ///     4. Otherwise → fail-closed unsupported storage. There is never a plaintext fallback.
    /// </summary>
    public static IServiceCollection AddPlatformCredentialStorage(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("FREEBOX_CREDENTIAL_FILE")))
        {
            services.AddFreeboxCredentialStorage<EncryptedFileCredentialStorage>();
        }
        else if (OperatingSystem.IsWindows())
        {
            services.AddFreeboxCredentialStorage<WindowsCredentialStorage>();
        }
        else if (LinuxSecretServiceCredentialStorage.IsSecretToolAvailable())
        {
            services.AddFreeboxCredentialStorage<LinuxSecretServiceCredentialStorage>();
        }
        else
        {
            services.AddFreeboxCredentialStorage<UnsupportedCredentialStorage>();
        }

        return services;
    }
}
