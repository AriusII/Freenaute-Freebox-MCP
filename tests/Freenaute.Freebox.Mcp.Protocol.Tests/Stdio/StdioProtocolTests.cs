namespace Freenaute.Freebox.Mcp.Protocol.Tests.Stdio;

public sealed class StdioProtocolTests
{
    private static readonly TimeSpan ResponseTimeout = TimeSpan.FromSeconds(20);

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
        JsonElement serverInfo = initialize.RootElement.GetProperty("result").GetProperty("serverInfo");
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
        string[] toolNames =
        [
            .. tools.RootElement.GetProperty("result").GetProperty("tools").EnumerateArray()
                .Select(element => element.GetProperty("name").GetString()!).Order()
        ];
        Assert.Equal(
        [
            "freebox_airmedia_receivers",
            "freebox_airmedia_status",
            "freebox_calls_account",
            "freebox_calls_clear_log",
            "freebox_calls_log",
            "freebox_calls_mark_all_read",
            "freebox_cameras_overview",
            "freebox_connection_status",
            "freebox_contacts_list",
            "freebox_contacts_search",
            "freebox_discover",
            "freebox_download_configuration",
            "freebox_download_files",
            "freebox_download_log",
            "freebox_download_pieces",
            "freebox_download_stats",
            "freebox_download_status",
            "freebox_downloads_list",
            "freebox_home_status",
            "freebox_lan_overview",
            "freebox_nas_visibility",
            "freebox_notifications_feed",
            "freebox_pairing_initiate",
            "freebox_pairing_status",
            "freebox_pairing_wait",
            "freebox_parental_config",
            "freebox_parental_filters",
            "freebox_shares_visibility",
            "freebox_status_feed",
            "freebox_system_status",
            "freebox_wifi_status"
        ], toolNames);

        JsonDocument resources = await SendAsync(process, input,
            "{\"jsonrpc\":\"2.0\",\"id\":3,\"method\":\"resources/list\",\"params\":{}}");
        string[] resourceUris =
        [
            .. resources.RootElement.GetProperty("result").GetProperty("resources").EnumerateArray()
                .Select(element => element.GetProperty("uri").GetString()!).Order()
        ];
        Assert.Equal(
        [
            "freebox://docs/en/auth-pairing",
            "freebox://docs/en/overview",
            "freebox://docs/en/quickstart",
            "freebox://docs/en/resources",
            "freebox://docs/en/tool-families",
            "freebox://docs/fr/authentification-appairage",
            "freebox://docs/fr/overview",
            "freebox://docs/fr/quickstart",
            "freebox://docs/fr/resources",
            "freebox://docs/fr/tool-families",
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
        bool hasStructuredLiveSnapshot = liveText.Contains("## Pairing", StringComparison.Ordinal);
        bool hasUnavailableFallback = liveText.Contains("Live data is unavailable", StringComparison.Ordinal)
                                      || liveText.Contains("Live data is currently unavailable.",
                                          StringComparison.Ordinal);
        Assert.True(hasStructuredLiveSnapshot || hasUnavailableFallback,
            "Expected either a structured live snapshot or a sanitized unavailability fallback.");
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
        start.Environment["FREEBOX_AUTHENTICATE_ON_STARTUP"] = "false";
        return Process.Start(start) ?? throw new InvalidOperationException("The MCP server did not start.");
    }

    private static async Task<JsonDocument> SendAsync(Process process, StreamWriter input, string json)
    {
        await input.WriteLineAsync(json.AsMemory(), TestContext.Current.CancellationToken);
        await input.FlushAsync(TestContext.Current.CancellationToken);
        string? line = await process.StandardOutput.ReadLineAsync(TestContext.Current.CancellationToken)
            .AsTask()
            .WaitAsync(ResponseTimeout, TestContext.Current.CancellationToken);
        if (!string.IsNullOrWhiteSpace(line))
        {
            return JsonDocument.Parse(line);
        }

        string stderr = await process.StandardError.ReadToEndAsync(TestContext.Current.CancellationToken);
        Assert.Fail($"The MCP server closed stdout without a JSON-RPC response. stderr: {stderr}");
        return null!;
    }
}
