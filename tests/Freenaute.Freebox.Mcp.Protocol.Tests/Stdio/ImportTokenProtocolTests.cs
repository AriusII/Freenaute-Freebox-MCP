namespace Freenaute.Freebox.Mcp.Protocol.Tests.Stdio;

public sealed class ImportTokenProtocolTests
{
    [Fact]
    public async Task ImportToken_RejectsTokenInArguments_WithoutEchoingSecret()
    {
        const string secret = "test-secret-that-must-not-be-echoed";
        ProcessResult result = await RunAsync(null, "import-token", "--token", secret);

        Assert.Equal(2, result.ExitCode);
        Assert.Contains("never accepted as arguments", result.StandardError, StringComparison.Ordinal);
        Assert.DoesNotContain(secret, result.StandardError, StringComparison.Ordinal);
        Assert.DoesNotContain(secret, result.StandardOutput, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UnknownCommand_DoesNotStartMcpProtocol()
    {
        ProcessResult result = await RunAsync(null, "not-a-command");

        Assert.Equal(2, result.ExitCode);
        Assert.Contains("Unknown command-line arguments", result.StandardError, StringComparison.Ordinal);
        Assert.Equal(string.Empty, result.StandardOutput);
    }

    [Fact]
    public async Task ImportToken_OnLinux_FailsClosedWithoutReadingOrEchoingStdin()
    {
        if (!OperatingSystem.IsLinux())
        {
            return;
        }

        const string secret = "linux-secret-that-must-never-be-used";
        ProcessResult result = await RunAsync(secret, "import-token", "--freebox-uid", "test-freebox");

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("fails closed", result.StandardError, StringComparison.Ordinal);
        Assert.DoesNotContain(secret, result.StandardError, StringComparison.Ordinal);
        Assert.DoesNotContain(secret, result.StandardOutput, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ImportToken_RequiresFreeboxUidBeforeReadingStdin()
    {
        const string secret = "stdin-secret-for-invalid-scope";
        ProcessResult result = await RunAsync(secret, "import-token", "--profile", "test");

        Assert.Equal(2, result.ExitCode);
        Assert.Contains("--freebox-uid", result.StandardError, StringComparison.Ordinal);
        Assert.DoesNotContain(secret, result.StandardError, StringComparison.Ordinal);
        Assert.DoesNotContain(secret, result.StandardOutput, StringComparison.Ordinal);
    }

    private static async Task<ProcessResult> RunAsync(string? stdin, params string[] arguments)
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
        if (stdin is not null)
        {
            await process.StandardInput.WriteLineAsync(stdin.AsMemory(), TestContext.Current.CancellationToken);
        }

        process.StandardInput.Close();
        string stdout = await process.StandardOutput.ReadToEndAsync(TestContext.Current.CancellationToken);
        string stderr = await process.StandardError.ReadToEndAsync(TestContext.Current.CancellationToken);
        await process.WaitForExitAsync(TestContext.Current.CancellationToken).WaitAsync(
            TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        return new ProcessResult(process.ExitCode, stdout, stderr);
    }

    private sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError);
}
