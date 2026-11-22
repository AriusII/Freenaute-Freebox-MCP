if (args.Length > 0)
{
    Console.Error.WriteLine("The MCP server uses no command-line arguments.");
    return 2;
}

if (!McpStartupEnvironment.TryRead(out McpStartupOptions startupOptions))
{
    return 2;
}

await McpHostBuilder.Build(startupOptions).RunAsync().ConfigureAwait(false);
return 0;
