namespace Freenaute.Freebox.Mcp.Tools.Tests.Notifications;

public sealed class NotificationToolTests
{
    [Fact]
    public async Task GetStatusFeedAsync_ReturnsSanitizedBoundedEntries()
    {
        var tools = new FreeboxNotificationTools(new NotificationOrchestrator());

        StatusFeedToolResult result = await tools.GetStatusFeedAsync(TestContext.Current.CancellationToken);

        Assert.Equal(5, result.Entries.Length);
        Assert.All(result.Entries, entry =>
        {
            Assert.NotEqual("pairing", entry.EntryId);
            Assert.DoesNotContain('\n', entry.Summary);
            Assert.True(entry.Summary.Length <= 160);
            Assert.True(entry.Topic.Length <= 32);
            Assert.True(entry.Level.Length <= 16);
        });
    }

    [Fact]
    public async Task GetNotificationsFeedAsync_MapsActionableSignals()
    {
        var tools = new FreeboxNotificationTools(new NotificationOrchestrator());

        NotificationFeedToolResult
            result = await tools.GetNotificationsFeedAsync(TestContext.Current.CancellationToken);

        Assert.Collection(result.Notifications,
            item =>
            {
                Assert.Equal("warning", item.Severity);
                Assert.Equal("CREDENTIAL_STATE", item.Code);
            },
            item =>
            {
                Assert.Equal("warning", item.Severity);
                Assert.Equal("CONNECTION_STATE", item.Code);
            },
            item =>
            {
                Assert.Equal("info", item.Severity);
                Assert.Equal("WIFI_DISABLED", item.Code);
            },
            item =>
            {
                Assert.Equal("warning", item.Severity);
                Assert.Equal("BOX_UNAUTHENTICATED", item.Code);
            },
            item =>
            {
                Assert.Equal("warning", item.Severity);
                Assert.Equal("DISK_STATUS", item.Code);
            });
    }

    [Fact]
    public async Task GetNotificationsFeedAsync_UsesAllClearWhenNoAlertsExist()
    {
        var tools = new FreeboxNotificationTools(new NotificationOrchestrator(true));

        NotificationFeedToolResult
            result = await tools.GetNotificationsFeedAsync(TestContext.Current.CancellationToken);

        NotificationFeedItemToolResult item = Assert.Single(result.Notifications);
        Assert.Equal("ALL_CLEAR", item.Code);
        Assert.Equal("info", item.Severity);
    }

    [Theory]
    [InlineData(false, true, true, "InvalidRequest")]
    [InlineData(true, false, true, "TransportFailure")]
    [InlineData(true, true, false, "Unknown")]
    public async Task Feeds_MapCoreErrorsToToolFailureMessages(
        bool connectionSuccess,
        bool networkSuccess,
        bool systemSuccess,
        string expectedCode)
    {
        var orchestrator = new NotificationOrchestrator(
            connectionSuccess: connectionSuccess,
            networkSuccess: networkSuccess,
            systemSuccess: systemSuccess);
        var tools = new FreeboxNotificationTools(orchestrator);

        InvalidOperationException statusException = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await tools.GetStatusFeedAsync(TestContext.Current.CancellationToken));
        InvalidOperationException notificationsException =
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await tools.GetNotificationsFeedAsync(TestContext.Current.CancellationToken));

        Assert.Contains(expectedCode, statusException.Message, StringComparison.Ordinal);
        Assert.Contains(expectedCode, notificationsException.Message, StringComparison.Ordinal);
    }

    private sealed class NotificationOrchestrator(
        bool allClear = false,
        bool connectionSuccess = true,
        bool networkSuccess = true,
        bool systemSuccess = true)
        : IFreeboxOrchestrator
    {
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
            return connectionSuccess
                ? ValueTask.FromResult(FreeboxCoreResults.Success(
                    new FreeboxConnectionResult("store", true,
                        allClear ? FreeboxCredentialState.Approved : FreeboxCredentialState.Missing)))
                : ValueTask.FromResult(FreeboxCoreResults.Failure<FreeboxConnectionResult>(
                    new FreeboxCoreError(FreeboxCoreErrorCode.InvalidRequest, "bad request")));
        }

        public ValueTask<FreeboxCoreResult<bool>> ForgetCredentialsAsync(CancellationToken cancellationToken = default)
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
            return networkSuccess
                ? ValueTask.FromResult(FreeboxCoreResults.Success(new FreeboxNetworkSnapshot(
                    new FreeboxConnectionSnapshot(
                        allClear ? "up" : "down\n",
                        "fiber",
                        "ethernet\n",
                        null, null, null, null, null, null, null, null, null, null, null),
                    new FreeboxLanSnapshot(
                        "lan",
                        "bridge",
                        "router",
                        [new FreeboxLanInterfaceSnapshot("main", 1)],
                        new Dictionary<string, FreeboxLanHostSnapshot[]>
                        {
                            ["main"] = [new FreeboxLanHostSnapshot("host-id", "pc", true, true, 42, "main")]
                        }),
                    new FreeboxWifiSnapshot(allClear, "off", allClear ? "up" : "disabled",
                        [new FreeboxWifiAccessPointSnapshot(1, "up")],
                        [new FreeboxWifiBssSnapshot("bss", 1, "up", 1, 1, true, "wpa2", false)]))))
                : ValueTask.FromResult(FreeboxCoreResults.Failure<FreeboxNetworkSnapshot>(
                    new FreeboxCoreError(FreeboxCoreErrorCode.TransportFailure, "transport error")));
        }

        public ValueTask<FreeboxCoreResult<FreeboxSystemSnapshot>> GetSystemSnapshotAsync(
            CancellationToken cancellationToken = default)
        {
            return systemSuccess
                ? ValueTask.FromResult(FreeboxCoreResults.Success(
                    new FreeboxSystemSnapshot(
                        "1.0",
                        "ultra",
                        "board",
                        20,
                        allClear,
                        true,
                        true,
                        allClear ? "healthy" : "failed",
                        [new FreeboxSystemSensorSnapshot("sensor", 1)],
                        [new FreeboxSystemSensorSnapshot("fan", 1)],
                        [new FreeboxSystemExpansionSnapshot(1, true, true, "pci")])))
                : ValueTask.FromResult(FreeboxCoreResults.Failure<FreeboxSystemSnapshot>(
                    new FreeboxCoreError(FreeboxCoreErrorCode.Unknown, "unknown failure")));
        }
    }
}
