namespace Freenaute.Freebox.Mcp.Hosting;

internal static partial class McpHostBuilder
{
    public static IHost Build(McpStartupOptions startupOptions)
    {
        ArgumentNullException.ThrowIfNull(startupOptions);

        HostApplicationBuilder builder = Host.CreateApplicationBuilder([]);

        // stdout carries the JSON-RPC stdio protocol; every log line goes to stderr only.
        builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);
        builder.Services.AddFreenauteFreeboxMcp(startupOptions);

        IHost host = builder.Build();
        ILogger logger = host.Services.GetRequiredService<ILoggerFactory>()
            .CreateLogger("Freenaute.Freebox.Mcp.Startup");
        LogHostBuilt(logger, startupOptions.AuthenticateOnStartup, startupOptions.AllowPairingInitiation,
            startupOptions.CredentialProfile);
        return host;
    }

    [LoggerMessage(
        EventId = 5000,
        Level = LogLevel.Information,
        Message =
            "MCP host built (authenticateOnStartup={AuthenticateOnStartup}, allowToolPairingInitiation={AllowPairingInitiation}, credentialProfile={CredentialProfile}).")]
    private static partial void LogHostBuilt(
        ILogger logger,
        bool? authenticateOnStartup,
        bool allowPairingInitiation,
        string credentialProfile);
}
