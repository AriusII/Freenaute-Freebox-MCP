namespace Freenaute.Freebox.Mcp.Tools.Tests.Shares;

public sealed class SharesToolTests
{
    [Fact]
    public async Task GetNasVisibilityAsync_MapsAndSanitizesSnapshot()
    {
        var orchestrator = new RecordingOrchestrator
        {
            SystemResult = FreeboxCoreResults.Success(new FreeboxSystemSnapshot(
                "v1.2.3\0",
                "Ultra",
                "board",
                12,
                true,
                true,
                true,
                "rea\0dy",
                [],
                [],
                []))
        };
        var tools = new FreeboxSharesTools(orchestrator);

        NasVisibilityToolResult result = await tools.GetNasVisibilityAsync(TestContext.Current.CancellationToken);

        Assert.True(result.UserStoragePowered);
        Assert.Equal("ready", result.DiskStatus);
        Assert.Equal("v1.2.3", result.FirmwareVersion);
        Assert.Equal("Ultra", result.Model);
    }

    [Fact]
    public async Task GetNasVisibilityAsync_OnFailure_ThrowsMappedException()
    {
        var orchestrator = new RecordingOrchestrator
        {
            SystemResult = FreeboxCoreResults.Failure<FreeboxSystemSnapshot>(new FreeboxCoreError(
                FreeboxCoreErrorCode.TransportFailure,
                "offline"))
        };
        var tools = new FreeboxSharesTools(orchestrator);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            tools.GetNasVisibilityAsync(TestContext.Current.CancellationToken));

        Assert.Contains("TransportFailure", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetSharesVisibilityAsync_WithHosts_MapsAggregatedVisibility()
    {
        const string RawInterfaceName = "lan-main\0side";
        var orchestrator = new RecordingOrchestrator
        {
            NetworkResult = FreeboxCoreResults.Success(new FreeboxNetworkSnapshot(
                new FreeboxConnectionSnapshot(null, null, null, null, null, null, null, null, null, null, null, null,
                    null, null),
                new FreeboxLanSnapshot(
                    "Home\0LAN",
                    "eth",
                    "bridge",
                    [new FreeboxLanInterfaceSnapshot(RawInterfaceName, 3)],
                    new Dictionary<string, FreeboxLanHostSnapshot[]>(StringComparer.Ordinal)
                    {
                        [RawInterfaceName] =
                        [
                            new FreeboxLanHostSnapshot("id-1", "desktop", true, true, null, RawInterfaceName),
                            new FreeboxLanHostSnapshot("id-2", "desktop", true, false, null, RawInterfaceName),
                            new FreeboxLanHostSnapshot("id-3", "desktop", false, false, null, RawInterfaceName)
                        ]
                    }),
                new FreeboxWifiSnapshot(null, null, null, [], [])))
        };
        var tools = new FreeboxSharesTools(orchestrator);

        SharesVisibilityToolResult result = await tools.GetSharesVisibilityAsync(
            true,
            null,
            TestContext.Current.CancellationToken);

        Assert.Equal("HomeLAN", result.LanName);
        Assert.Equal(1, result.InterfaceCount);
        Assert.Equal(2, result.ReachableHostCount);
        Assert.Equal(1, result.ActiveHostCount);
        ShareInterfaceVisibilityToolResult mapped = Assert.Single(result.Interfaces);
        Assert.Equal("lan-mainside", mapped.Name);
        Assert.Equal(3, mapped.HostCount);
        Assert.Equal(2, mapped.ReachableHostCount);
        Assert.Equal(1, mapped.ActiveHostCount);
    }

    [Fact]
    public async Task GetSharesVisibilityAsync_InvalidInterfaceName_Throws()
    {
        var tools = new FreeboxSharesTools(new RecordingOrchestrator());

        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await tools.GetSharesVisibilityAsync(
                false,
                "bad\0name",
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetSharesVisibilityAsync_OnFailure_ThrowsMappedException()
    {
        var orchestrator = new RecordingOrchestrator
        {
            NetworkResult = FreeboxCoreResults.Failure<FreeboxNetworkSnapshot>(new FreeboxCoreError(
                FreeboxCoreErrorCode.ApiRejected,
                "denied"))
        };
        var tools = new FreeboxSharesTools(orchestrator);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            tools.GetSharesVisibilityAsync(cancellationToken: TestContext.Current.CancellationToken));

        Assert.Contains("ApiRejected", exception.Message, StringComparison.Ordinal);
    }

    private sealed class RecordingOrchestrator : IFreeboxOrchestrator
    {
        public FreeboxCoreResult<FreeboxNetworkSnapshot> NetworkResult { get; set; } =
            FreeboxCoreResults.Success(new FreeboxNetworkSnapshot(
                new FreeboxConnectionSnapshot(null, null, null, null, null, null, null, null, null, null, null, null,
                    null, null),
                new FreeboxLanSnapshot(null, null, null, [], new Dictionary<string, FreeboxLanHostSnapshot[]>()),
                new FreeboxWifiSnapshot(null, null, null, [], [])));

        public FreeboxCoreResult<FreeboxSystemSnapshot> SystemResult { get; set; } =
            FreeboxCoreResults.Success(new FreeboxSystemSnapshot(null, null, null, null, null, null, null, null, [],
                [], []));

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
            throw new NotSupportedException();
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
            return ValueTask.FromResult(NetworkResult);
        }

        public ValueTask<FreeboxCoreResult<FreeboxSystemSnapshot>> GetSystemSnapshotAsync(
            CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(SystemResult);
        }
    }
}
