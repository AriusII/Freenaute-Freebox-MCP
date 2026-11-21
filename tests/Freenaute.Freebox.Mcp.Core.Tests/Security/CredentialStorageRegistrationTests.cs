#if HAS_CREDENTIAL_STORAGE_REGISTRATION
using Microsoft.Extensions.DependencyInjection;

namespace Freenaute.Freebox.Mcp.Core.Tests.Security;

[Collection(CredentialEnvironment.Name)]
public sealed class CredentialStorageRegistrationTests : IDisposable
{
    private readonly string? _previousCredentialFile =
        Environment.GetEnvironmentVariable("FREEBOX_CREDENTIAL_FILE");

    public void Dispose()
    {
        Environment.SetEnvironmentVariable("FREEBOX_CREDENTIAL_FILE", _previousCredentialFile);
    }

    [Fact]
    public void AddPlatformCredentialStorage_RegistersExactlyOneCredentialStorage()
    {
        Environment.SetEnvironmentVariable("FREEBOX_CREDENTIAL_FILE", null);
        var services = new ServiceCollection();

        services.AddPlatformCredentialStorage();

        Assert.Equal(1, services.Count(descriptor => descriptor.ServiceType == typeof(IFreeboxCredentialStorage)));
        using ServiceProvider provider = services.BuildServiceProvider();
        IFreeboxCredentialStorage storage = provider.GetRequiredService<IFreeboxCredentialStorage>();
        Assert.NotNull(storage);
        Assert.False(string.IsNullOrWhiteSpace(storage.Kind));
    }

    [Fact]
    public void AddPlatformCredentialStorage_SelectsExpectedBackendPerPlatform()
    {
        Environment.SetEnvironmentVariable("FREEBOX_CREDENTIAL_FILE", null);
        var services = new ServiceCollection();

        services.AddPlatformCredentialStorage();

        ServiceDescriptor descriptor = services.Single(d => d.ServiceType == typeof(IFreeboxCredentialStorage));
        Assert.NotNull(descriptor.ImplementationType);
        string implementationName = descriptor.ImplementationType.Name;
        if (OperatingSystem.IsWindows())
        {
            Assert.Equal(typeof(WindowsCredentialStorage), descriptor.ImplementationType);
        }
        else if (OperatingSystem.IsLinux())
        {
            Assert.Equal(
                LinuxSecretServiceCredentialStorage.IsSecretToolAvailable()
                    ? "LinuxSecretServiceCredentialStorage"
                    : "UnsupportedCredentialStorage",
                implementationName);
        }
        else if (OperatingSystem.IsMacOS())
        {
            // The macOS keychain backend was removed; there is no plaintext fallback.
            Assert.Equal("UnsupportedCredentialStorage", implementationName);
        }
    }

    [Fact]
    public void AddPlatformCredentialStorage_CredentialFileEnvWinsOnAnyPlatform()
    {
        string temporaryFile = Path.Combine(Path.GetTempPath(), "freenaute-registration-" +
            Guid.NewGuid().ToString("N") + ".bin");
        Environment.SetEnvironmentVariable("FREEBOX_CREDENTIAL_FILE", temporaryFile);
        try
        {
            var services = new ServiceCollection();

            services.AddPlatformCredentialStorage();

            ServiceDescriptor descriptor =
                services.Single(d => d.ServiceType == typeof(IFreeboxCredentialStorage));
            Assert.Equal("EncryptedFileCredentialStorage", descriptor.ImplementationType?.Name);
        }
        finally
        {
            Environment.SetEnvironmentVariable("FREEBOX_CREDENTIAL_FILE", _previousCredentialFile);
        }
    }
}
#endif
