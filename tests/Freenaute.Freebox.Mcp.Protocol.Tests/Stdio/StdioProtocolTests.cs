namespace Freenaute.Freebox.Mcp.Protocol.Tests.Stdio;

public sealed class StdioProtocolTests
{
    [Fact]
    public async Task ManagedStdioServer_InitializeAndListsExpectedCapabilities_WithoutFreebox()
    {
        string assembly = McpHostLocator.FindHostAssembly();
        using Process process = StartServer(assembly);
        await using StreamWriter input = process.StandardInput;
        JsonDocument initialize = await SendAsync(process, input,
            """
            {"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-06-18","capabilities":{},"clientInfo":{"name":"tests","version":"1.0"}}}
            """);
        var serverInfo = initialize.RootElement.GetProperty("result").GetProperty("serverInfo");
        Assert.Equal("Freenaute.Freebox.Mcp", serverInfo.GetProperty("name").GetString());

        // serverInfo.version must be the version of the host build (BuildInfo.Version from MSBuild).
        string? version = serverInfo.GetProperty("version").GetString();
        Assert.False(string.IsNullOrWhiteSpace(version));
        var buildInfo = FileVersionInfo.GetVersionInfo(assembly);
        string? productVersionBase = buildInfo.ProductVersion?.Split('+')[0];
        Assert.True(
            string.Equals(version, buildInfo.FileVersion, StringComparison.Ordinal) ||
            string.Equals(version, productVersionBase, StringComparison.Ordinal),
            $"serverInfo.version '{version}' does not match the host build version " +
            $"(file: {buildInfo.FileVersion}, product: {buildInfo.ProductVersion}).");

        await input.WriteLineAsync("{\"jsonrpc\":\"2.0\",\"method\":\"notifications/initialized\"}");
        await input.FlushAsync(TestContext.Current.CancellationToken);

        JsonDocument tools = await SendAsync(process, input,
            "{\"jsonrpc\":\"2.0\",\"id\":2,\"method\":\"tools/list\",\"params\":{}}");
        string[] toolNames = tools.RootElement.GetProperty("result").GetProperty("tools").EnumerateArray()
            .Select(element => element.GetProperty("name").GetString()!).Order().ToArray();
        Assert.Equal(
        [
            "freebox_connection_status",
            "freebox_discover",
            "freebox_lan_overview",
            "freebox_pairing_initiate",
            "freebox_pairing_status",
            "freebox_pairing_wait",
            "freebox_system_status",
            "freebox_wifi_status"
        ], toolNames);

        JsonDocument resources = await SendAsync(process, input,
            "{\"jsonrpc\":\"2.0\",\"id\":3,\"method\":\"resources/list\",\"params\":{}}");
        string[] resourceUris = resources.RootElement.GetProperty("result").GetProperty("resources").EnumerateArray()
            .Select(element => element.GetProperty("uri").GetString()!).Order().ToArray();
        Assert.Equal(
        [
            "freebox://docs/en/overview",
            "freebox://docs/en/resources",
            "freebox://docs/fr/overview",
            "freebox://docs/fr/resources",
            "freebox://live/status"
        ], resourceUris);

        JsonDocument read = await SendAsync(process, input,
            "{\"jsonrpc\":\"2.0\",\"id\":4,\"method\":\"resources/read\",\"params\":{\"uri\":\"freebox://docs/en/overview\"}}");
        JsonElement contents = read.RootElement.GetProperty("result").GetProperty("contents");
        Assert.Contains("Freebox", contents[0].GetProperty("text").GetString(), StringComparison.Ordinal);

        JsonDocument live = await SendAsync(process, input,
            "{\"jsonrpc\":\"2.0\",\"id\":5,\"method\":\"resources/read\",\"params\":{\"uri\":\"freebox://live/status\"}}");
        string liveText = live.RootElement.GetProperty("result").GetProperty("contents")[0]
            .GetProperty("text").GetString()!;
        Assert.Contains("# Freebox status", liveText, StringComparison.Ordinal);
        Assert.Contains("## Pairing", liveText, StringComparison.Ordinal);
        Assert.DoesNotContain("no `IFreeboxLiveResourceSource` is registered", liveText, StringComparison.Ordinal);

        JsonDocument shutdown = await SendAsync(process, input,
            "{\"jsonrpc\":\"2.0\",\"id\":6,\"method\":\"shutdown\"}");
        Assert.Equal(6, shutdown.RootElement.GetProperty("id").GetInt32());
    }

    private static Process StartServer(string assembly)
    {
        Assert.True(File.Exists(assembly), $"Managed server assembly was not found at '{assembly}'.");

        var start = new ProcessStartInfo("dotnet")
        {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        start.ArgumentList.Add(assembly);
        start.Environment["DOTNET_ENVIRONMENT"] = "Production";
        start.Environment["FREEBOX_APPLICATION_ID"] = "org.freenaute.freebox-mcp.tests";
        return Process.Start(start) ?? throw new InvalidOperationException("The MCP server did not start.");
    }

    private static async Task<JsonDocument> SendAsync(Process process, StreamWriter input, string json)
    {
        await input.WriteLineAsync(json.AsMemory(), TestContext.Current.CancellationToken);
        await input.FlushAsync(TestContext.Current.CancellationToken);
        string? line = await process.StandardOutput.ReadLineAsync(TestContext.Current.CancellationToken)
            .AsTask()
            .WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        if (!string.IsNullOrWhiteSpace(line))
        {
            return JsonDocument.Parse(line);
        }

        string stderr = await process.StandardError.ReadToEndAsync(TestContext.Current.CancellationToken);
        Assert.Fail($"The MCP server closed stdout without a JSON-RPC response. stderr: {stderr}");
        return null!;
    }
}
