namespace Freenaute.Freebox.Mcp.Hosting;

internal static class McpHostBuilder
{
    public static IHost Build(McpStartupOptions startupOptions)
    {
        ArgumentNullException.ThrowIfNull(startupOptions);

        HostApplicationBuilder builder = Host.CreateApplicationBuilder([]);

        // stdout carries the JSON-RPC stdio protocol; every log line goes to stderr only.
        builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);
        builder.Services.AddFreenauteFreeboxMcp(startupOptions);

        return builder.Build();
    }
}
