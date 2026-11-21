namespace Freenaute.Freebox.Mcp.Core.DependencyInjection;

public static class FreeboxMcpCoreServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddFreeboxMcpCore(Action<FreeboxCoreOptions>? configure = null)
        {
            ArgumentNullException.ThrowIfNull(services);

            var options = new FreeboxCoreOptions();
            configure?.Invoke(options);
            ValidateOptions(options);

            services.AddOptions<FreeboxCoreOptions>()
                .Configure(current => Copy(options, current))
                .ValidateOnStart();

            services.AddFreeboxClient(client => Copy(options, client), true);
            services.AddSingleton<IFreeboxOrchestrator, FreeboxOrchestrator>();

            return services;
        }

        public IServiceCollection AddFreeboxCredentialStorage<
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
            TStorage>()
            where TStorage : class, IFreeboxCredentialStorage
        {
            ArgumentNullException.ThrowIfNull(services);
            services.AddSingleton<IFreeboxCredentialStorage, TStorage>();
            services.AddSingleton<IFreeboxCredentialStore, FreeboxCredentialStoreAdapter>();
            return services;
        }
    }

    private static void Copy(FreeboxCoreOptions source, FreeboxCoreOptions target)
    {
        target.ServerAddress = source.ServerAddress;
        target.ApplicationId = source.ApplicationId;
        target.ApplicationName = source.ApplicationName;
        target.ApplicationVersion = source.ApplicationVersion;
        target.DeviceName = source.DeviceName;
        target.CredentialProfile = source.CredentialProfile;
        target.RequestTimeout = source.RequestTimeout;
        target.EnableMdnsDiscovery = source.EnableMdnsDiscovery;
        target.MdnsTimeout = source.MdnsTimeout;
        target.EnableResilience = source.EnableResilience;
        target.AllowPairingInitiation = source.AllowPairingInitiation;
    }

    private static void Copy(FreeboxCoreOptions source, FreeboxClientOptions target)
    {
        target.ServerAddress = source.ServerAddress;
        target.Timeout = source.RequestTimeout;
        target.EnableMdnsDiscovery = source.EnableMdnsDiscovery;
        target.MdnsTimeout = source.MdnsTimeout;
        target.ApplicationId = source.ApplicationId;
        target.ApplicationName = source.ApplicationName;
        target.ApplicationVersion = source.ApplicationVersion;
        target.DeviceName = source.DeviceName;
        target.CredentialProfile = source.CredentialProfile;
        target.EnableResilience = source.EnableResilience;
    }

    private static void ValidateOptions(FreeboxCoreOptions options)
    {
        ArgumentNullException.ThrowIfNull(options.ServerAddress);
        if (!options.ServerAddress.IsAbsoluteUri ||
            (options.ServerAddress.Scheme != Uri.UriSchemeHttp &&
             options.ServerAddress.Scheme != Uri.UriSchemeHttps) ||
            !string.IsNullOrEmpty(options.ServerAddress.UserInfo) ||
            !string.IsNullOrEmpty(options.ServerAddress.Query) ||
            !string.IsNullOrEmpty(options.ServerAddress.Fragment) ||
            !string.Equals(options.ServerAddress.AbsolutePath, "/", StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "ServerAddress must be an absolute HTTP(S) origin without a path, credentials, query or fragment.",
                nameof(options));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(options.ApplicationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.ApplicationName);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.ApplicationVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.DeviceName);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.CredentialProfile);
    }
}
