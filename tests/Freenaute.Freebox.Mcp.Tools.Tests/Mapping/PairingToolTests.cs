namespace Freenaute.Freebox.Mcp.Tools.Tests.Mapping;

public sealed class PairingToolTests
{
    [Fact]
    public async Task WaitAsync_ForwardsExplicitTimingValuesToOrchestrator()
    {
        var orchestrator = new RecordingOrchestrator();
        var tools = new FreeboxPairingTools(orchestrator);

        PairingApprovalToolResult result = await tools.WaitAsync(
            3,
            30,
            TestContext.Current.CancellationToken);

        Assert.True(result.Completed);
        Assert.Equal(TimeSpan.FromSeconds(3), orchestrator.LastPollInterval);
        Assert.Equal(TimeSpan.FromSeconds(30), orchestrator.LastTimeout);
    }

    [Theory]
    [InlineData(0, null)]
    [InlineData(-1, null)]
    [InlineData(null, 0)]
    [InlineData(null, -1)]
    public async Task WaitAsync_InvalidTimingValues_AreRejected(int? pollIntervalSeconds, int? timeoutSeconds)
    {
        var tools = new FreeboxPairingTools(new RecordingOrchestrator());

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await tools.WaitAsync(
                pollIntervalSeconds,
                timeoutSeconds,
                TestContext.Current.CancellationToken));

        Assert.Contains("InvalidRequest", exception.Message, StringComparison.Ordinal);
    }

    private sealed class RecordingOrchestrator : IFreeboxOrchestrator
    {
        public TimeSpan? LastPollInterval { get; private set; }
        public TimeSpan? LastTimeout { get; private set; }

        public ValueTask<FreeboxCoreResult<FreeboxPairingResult>> BeginPairingAsync(
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public ValueTask<FreeboxCoreResult<FreeboxCredentialResult>> WaitForApprovalAsync(
            TimeSpan? pollInterval = null,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default)
        {
            LastPollInterval = pollInterval;
            LastTimeout = timeout;
            return ValueTask.FromResult(FreeboxCoreResults.Success(
                new FreeboxCredentialResult(FreeboxCredentialState.Approved)));
        }

        public ValueTask<FreeboxCoreResult<FreeboxConnectionResult>> GetConnectionStatusAsync(
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public ValueTask<FreeboxCoreResult<bool>> ForgetCredentialsAsync(
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public ValueTask<FreeboxCoreResult<FreeboxDiscoverySnapshot>> DiscoverAsync(
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public ValueTask<FreeboxCoreResult<FreeboxNetworkSnapshot>> GetNetworkSnapshotAsync(
            bool includeHosts = false,
            string? interfaceName = null,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public ValueTask<FreeboxCoreResult<FreeboxSystemSnapshot>> GetSystemSnapshotAsync(
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
}
