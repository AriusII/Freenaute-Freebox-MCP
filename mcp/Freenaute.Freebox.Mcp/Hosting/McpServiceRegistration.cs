namespace Freenaute.Freebox.Mcp.Hosting;

internal static class McpServiceRegistration
{
    public static IServiceCollection AddFreenauteFreeboxMcp(
        this IServiceCollection services,
        McpStartupOptions startupOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(startupOptions);

        services.AddFreeboxMcpCore(options =>
        {
            options.ApplicationId = startupOptions.ApplicationId;
            options.ApplicationName = startupOptions.ApplicationName;
            options.DeviceName = startupOptions.DeviceName;
            options.CredentialProfile = startupOptions.CredentialProfile;
            options.AllowPairingInitiation = startupOptions.AllowPairingInitiation;

            if (startupOptions.ServerAddress is not null)
            {
                options.ServerAddress = startupOptions.ServerAddress;
            }

            if (startupOptions.PairingPollIntervalSeconds is not null)
            {
                options.PairingPollInterval = TimeSpan.FromSeconds(startupOptions.PairingPollIntervalSeconds.Value);
            }

            if (startupOptions.PairingWaitTimeoutSeconds is not null)
            {
                options.PairingWaitTimeout = TimeSpan.FromSeconds(startupOptions.PairingWaitTimeoutSeconds.Value);
            }

            if (startupOptions.AuthenticateOnStartup is not null)
            {
                options.AuthenticateOnStartup = startupOptions.AuthenticateOnStartup.Value;
            }
        });
        services.AddPlatformCredentialStorage();
        services.AddFreeboxStartupAuthentication();
        services.AddProductionFreeboxLiveResourceSource();
        services.AddSingleton<IFreeboxCallsService, FreeboxCallsService>();

        JsonSerializerOptions serializerOptions = ToolResultJsonSerializerContext.Default.Options;
        services
            .AddMcpServer(options =>
            {
                options.ServerInfo = new Implementation
                {
                    Name = "Freenaute.Freebox.Mcp",
                    Version = BuildInfo.Version
                };
                options.ServerInstructions = "Pairing initiation is disabled unless explicitly enabled by host policy.";
            })
            .WithStdioServerTransport()
            .WithTools<FreeboxDiscoveryTools>(serializerOptions)
            .WithTools<FreeboxDownloadsTools>(serializerOptions)
            .WithTools<FreeboxCallTools>(serializerOptions)
            .WithTools<FreeboxCamerasTools>(serializerOptions)
            .WithTools<FreeboxContactsTools>(serializerOptions)
            .WithTools<FreeboxAirMediaTools>(serializerOptions)
            .WithTools<FreeboxSharesTools>(serializerOptions)
            .WithTools<FreeboxPairingTools>(serializerOptions)
            .WithTools<FreeboxParentalTools>(serializerOptions)
            .WithTools<FreeboxSystemTools>(serializerOptions)
            .WithTools<FreeboxNetworkTools>(serializerOptions)
            .WithTools<FreeboxNotificationTools>(serializerOptions)
            .AddFreeboxResources();

        return services;
    }
}
