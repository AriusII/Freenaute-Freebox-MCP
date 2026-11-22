namespace Freenaute.Freebox.Mcp.Tools.Tests.Network;

public sealed class AdvancedNetworkToolTests
{
    [Fact]
    public async Task DeletePortForwardAsync_WithoutConfirmation_DoesNotAccessClient()
    {
        IFreeboxClient client = DispatchProxy.Create<IFreeboxClient, TrackingClientProxy>()!;
        var proxy = (TrackingClientProxy)client;
        var tools = new FreeboxAdvancedNetworkTools(client);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await tools.DeletePortForwardAsync(10, false, TestContext.Current.CancellationToken));

        Assert.Contains("InvalidRequest", exception.Message, StringComparison.Ordinal);
        Assert.Contains("confirm", exception.Message, StringComparison.Ordinal);
        Assert.Equal(0, proxy.NetworkAccessCount);
    }

    [Fact]
    public async Task CreatePortForwardAsync_WithInvalidProtocol_DoesNotAccessClient()
    {
        IFreeboxClient client = DispatchProxy.Create<IFreeboxClient, TrackingClientProxy>()!;
        var proxy = (TrackingClientProxy)client;
        var tools = new FreeboxAdvancedNetworkTools(client);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await tools.CreatePortForwardAsync(
                "icmp",
                443,
                "192.168.1.10",
                443,
                cancellationToken: TestContext.Current.CancellationToken));

        Assert.Contains("protocol", exception.Message, StringComparison.Ordinal);
        Assert.Equal(0, proxy.NetworkAccessCount);
    }

    [SuppressMessage(
        "Performance", "CA1852:Seal internal types",
        Justification = "DispatchProxy derives a runtime-generated proxy from this base type.")]
    private class TrackingClientProxy : DispatchProxy
    {
        public int NetworkAccessCount { get; private set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == "get_Network")
            {
                NetworkAccessCount++;
                throw new InvalidOperationException("Network access was not expected.");
            }

            throw new InvalidOperationException("Unexpected client member invocation.");
        }
    }
}
