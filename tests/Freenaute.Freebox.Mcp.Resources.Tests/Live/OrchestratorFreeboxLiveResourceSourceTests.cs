namespace Freenaute.Freebox.Mcp.Resources.Tests.Live;

public sealed class OrchestratorFreeboxLiveResourceSourceTests
{
    [Fact]
    public async Task GetSnapshotAsync_UsesOnlyAllowListedBoundedValues()
    {
        var source = new OrchestratorFreeboxLiveResourceSource(new StubOrchestrator());

        FreeboxLiveSnapshot snapshot =
            await source.GetSnapshotAsync(TestContext.Current.CancellationToken);

        Assert.Equal("text/markdown", snapshot.MediaType);
        Assert.Contains("connection state: up", snapshot.Content, StringComparison.Ordinal);
        Assert.Contains("uploaded bytes: 42", snapshot.Content, StringComparison.Ordinal);
        Assert.Contains("uptime (seconds): 3600", snapshot.Content, StringComparison.Ordinal);
        Assert.Contains("sensor count: 16", snapshot.Content, StringComparison.Ordinal);
        string[] forbidden =
        [
            "app-secret-token", "private uid", "192.0.2.10", "2001:db8::1", "AA-BB-CC-DD-EE-FF",
            "Private WiFi", "private-host.local", "private-lan-name"
        ];
        foreach (string value in forbidden)
        {
            Assert.DoesNotContain(value, snapshot.Content, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task GetSnapshotAsync_ReportsUnavailableWithoutLeakingErrors()
    {
        var source = new OrchestratorFreeboxLiveResourceSource(new StubOrchestrator());

        FreeboxLiveSnapshot snapshot =
            await source.GetSnapshotAsync(TestContext.Current.CancellationToken);

        Assert.Contains("availability: unavailable", snapshot.Content, StringComparison.Ordinal);
    }

    private sealed class StubOrchestrator : IFreeboxOrchestrator
    {
        public ValueTask<FreeboxCoreResult<FreeboxConnectionResult>> GetConnectionStatusAsync(
            CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(CreateFailure<FreeboxConnectionResult>(
                new FreeboxCoreError(FreeboxCoreErrorCode.Unknown, "app-secret-token private uid")));
        }

        public ValueTask<FreeboxCoreResult<FreeboxNetworkSnapshot>> GetNetworkSnapshotAsync(
            bool includeHosts = false, string? interfaceName = null, CancellationToken cancellationToken = default)
        {
            var snapshot = new FreeboxNetworkSnapshot(
                new FreeboxConnectionSnapshot("up", "ethernet", "ftth", null, null, null, null, 42, null,
                    null, null, null, null, null),
                new FreeboxLanSnapshot("private-lan-name", null, null,
                    [new FreeboxLanInterfaceSnapshot("private-host.local", 1)],
                    new Dictionary<string, FreeboxLanHostSnapshot[]>()),
                new FreeboxWifiSnapshot(true, null, "enabled", new FreeboxWifiAccessPointSnapshot[1],
                    new FreeboxWifiBssSnapshot[1]));
            return ValueTask.FromResult(CreateSuccess(snapshot));
        }

        public ValueTask<FreeboxCoreResult<FreeboxSystemSnapshot>> GetSystemSnapshotAsync(
            CancellationToken cancellationToken = default)
        {
            var snapshot = new FreeboxSystemSnapshot(
                "firmware", "model", "AA-BB-CC-DD-EE-FF private uid", 3600, null, null, null, null,
                Enumerable.Range(0, 20).Select(_ => new FreeboxSystemSensorSnapshot("id", null)).ToArray(),
                [], []);
            return ValueTask.FromResult(CreateSuccess(snapshot));
        }

        public ValueTask<FreeboxCoreResult<FreeboxPairingResult>> BeginPairingAsync(
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public ValueTask<FreeboxCoreResult<FreeboxCredentialResult>> WaitForApprovalAsync(
            TimeSpan? pollInterval = null, CancellationToken cancellationToken = default)
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

        private static FreeboxCoreResult<T> CreateSuccess<T>(T value)
        {
            return FreeboxCoreResults.Success(value);
        }

        private static FreeboxCoreResult<T> CreateFailure<T>(FreeboxCoreError error)
        {
            return FreeboxCoreResults.Failure<T>(error);
        }
    }
}
