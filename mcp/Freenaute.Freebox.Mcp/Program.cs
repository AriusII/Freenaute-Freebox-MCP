using Freenaute.Freebox.Mcp;

if (args.Length > 0)
{
    Console.Error.WriteLine("Unknown command-line arguments. The MCP server uses no command-line options.");
    return 2;
}

Uri? serverAddress = null;
string? serverAddressValue = Environment.GetEnvironmentVariable("FREEBOX_SERVER_ADDRESS");
if (!string.IsNullOrWhiteSpace(serverAddressValue))
{
    if (!Uri.TryCreate(serverAddressValue, UriKind.Absolute, out serverAddress) ||
        serverAddress.Scheme is not ("http" or "https"))
    {
        Console.Error.WriteLine(
            "FREEBOX_SERVER_ADDRESS must be an absolute http(s) URI, for example https://mafreebox.freebox.fr/.");
        return 2;
    }
}

JsonSerializerOptions serializerOptions = ToolResultJsonSerializerContext.Default.Options;
HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

// stdout carries the JSON-RPC stdio protocol; every log line goes to stderr only.
builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);

bool allowPairingInitiation = PairingPolicy.IsEnabled(
    Environment.GetEnvironmentVariable("FREEBOX_ALLOW_PAIRING_INITIATION"));

builder.Services.AddFreeboxMcpCore(options =>
{
    options.ApplicationId = Environment.GetEnvironmentVariable("FREEBOX_APPLICATION_ID") ??
                            BuildInfo.ApplicationId;
    options.ApplicationName = Environment.GetEnvironmentVariable("FREEBOX_APPLICATION_NAME") ?? "Freenaute Freebox MCP";
    options.DeviceName = Environment.GetEnvironmentVariable("FREEBOX_DEVICE_NAME") ?? Environment.MachineName;
    options.CredentialProfile = Environment.GetEnvironmentVariable("FREEBOX_CREDENTIAL_PROFILE") ?? "default";
    if (serverAddress is not null)
    {
        options.ServerAddress = serverAddress;
    }

    options.AllowPairingInitiation = allowPairingInitiation;
});
builder.Services.AddPlatformCredentialStorage();
builder.Services.AddProductionFreeboxLiveResourceSource();

builder.Services
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
    .WithTools<FreeboxPairingTools>(serializerOptions)
    .WithTools<FreeboxSystemTools>(serializerOptions)
    .WithTools<FreeboxNetworkTools>(serializerOptions)
    .AddFreeboxResources();

await builder.Build().RunAsync();
return 0;
