namespace Freenaute.Freebox.Mcp.Protocol.Tests.Stdio;

public sealed class ArgumentRejectionProtocolTests
{
    [Theory]
    [InlineData("import-token")]
    [InlineData("--unexpected")]
    [InlineData("--help")]
    [InlineData("not-a-command")]
    public async Task AnyLaunchArgument_IsRejectedWithoutStartingProtocol(string argument)
    {
        ProcessResult result = await RunAsync(argument);

        Assert.Equal(2, result.ExitCode);
        Assert.Contains("uses no command-line arguments", result.StandardError, StringComparison.Ordinal);
        Assert.Equal(string.Empty, result.StandardOutput);
    }

    [Fact]
    public async Task ArgumentPayload_IsNotEchoed()
    {
        const string secret = "test-secret-that-must-not-be-echoed";
        ProcessResult result = await RunAsync("--unexpected", secret);

        Assert.Equal(2, result.ExitCode);
        Assert.Contains("uses no command-line arguments", result.StandardError, StringComparison.Ordinal);
        Assert.DoesNotContain(secret, result.StandardError, StringComparison.Ordinal);
        Assert.DoesNotContain(secret, result.StandardOutput, StringComparison.Ordinal);
        Assert.Equal(string.Empty, result.StandardOutput);
    }

    private static async Task<ProcessResult> RunAsync(params string[] arguments)
    {
        string assembly = McpHostLocator.FindHostAssembly();
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
        foreach (string argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }

        using Process process =
            Process.Start(start) ?? throw new InvalidOperationException("The MCP host did not start.");
        process.StandardInput.Close();
        string stdout = await process.StandardOutput.ReadToEndAsync(TestContext.Current.CancellationToken);
        string stderr = await process.StandardError.ReadToEndAsync(TestContext.Current.CancellationToken);
        await process.WaitForExitAsync(TestContext.Current.CancellationToken).WaitAsync(
            TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        return new ProcessResult(process.ExitCode, stdout, stderr);
    }

    private sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError);
}
