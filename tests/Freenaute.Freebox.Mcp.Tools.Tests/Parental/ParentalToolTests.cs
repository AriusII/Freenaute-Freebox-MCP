namespace Freenaute.Freebox.Mcp.Tools.Tests.Parental;

public sealed class ParentalToolTests
{
    [Fact]
    public async Task GetConfigAndFiltersAsync_ReturnsSanitizedParentalData()
    {
        IFreeboxClient client = DispatchProxy.Create<IFreeboxClient, FreeboxClientProxy>()!;
        var clientProxy = (FreeboxClientProxy)client;
        clientProxy.Config = new ParentalFilterConfig
        {
            DefaultFilterMode = ParentalFilterState.Allowed
        };
        clientProxy.Filters =
        [
            new ParentalFilter
            {
                Id = 42,
                Macs = ["AA:BB:CC:DD:EE:FF", "11:22:33:44:55:66"],
                Hosts = ["laptop.local"],
                Ip = "192.168.1.10",
                Desc = "Child tablet",
                Forced = true,
                ForcedMode = ParentalFilterState.Webonly,
                TmpModeExpire = 1700000000,
                TmpMode = ParentalFilterState.Denied,
                SchedulingMode = ParentalSchedulingMode.Temporary,
                FilterState = ParentalFilterState.Allowed,
                CurrentMappingIdx = 3,
                NextChange = 1700000100
            }
        ];
        var tools = new FreeboxParentalTools(client);

        ParentalConfigToolResult config = await tools.GetConfigAsync(TestContext.Current.CancellationToken);
        ParentalFiltersToolResult filters = await tools.GetFiltersAsync(10, TestContext.Current.CancellationToken);

        Assert.Equal("allowed", config.DefaultFilterMode);
        Assert.Equal(1, filters.TotalCount);
        ParentalFilterToolResult first = Assert.Single(filters.Filters);
        Assert.NotEqual("42", first.StableId);
        Assert.Equal(
            Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes("42"))),
            first.StableId);
        Assert.Equal(2, first.MacAddressCount);
        Assert.Equal(1, first.HostCount);
        Assert.True(first.HasIpAddress);
        Assert.True(first.HasDescription);
        Assert.Equal("webonly", first.ForcedMode);
        Assert.Equal("temporary", first.SchedulingMode);
    }

    [Fact]
    public async Task GetFiltersAsync_WhenApiThrows_MapsFailureToToolError()
    {
        IFreeboxClient client = DispatchProxy.Create<IFreeboxClient, FreeboxClientProxy>()!;
        var clientProxy = (FreeboxClientProxy)client;
        clientProxy.FiltersException = new InvalidOperationException("sensitive detail");
        var tools = new FreeboxParentalTools(client);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await tools.GetFiltersAsync(cancellationToken: TestContext.Current.CancellationToken));

        Assert.Contains("InvalidRequest", exception.Message, StringComparison.Ordinal);
    }

    [SuppressMessage(
        "Performance", "CA1852:Seal internal types",
        Justification = "DispatchProxy derives a runtime-generated proxy from this base type.")]
    private class FreeboxClientProxy : DispatchProxy
    {
        public ParentalFilterConfig Config { get; set; } = new();
        public ParentalFilter[] Filters { get; set; } = [];
        public Exception? FiltersException { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            return targetMethod?.Name switch
            {
                "get_Parental" => CreateParentalProxy(),
                _ => throw new InvalidOperationException("Only parental API access is expected in parental tool tests.")
            };
        }

        private IFreeboxParentalApi CreateParentalProxy()
        {
            IFreeboxParentalApi parental = Create<IFreeboxParentalApi, FreeboxParentalApiProxy>()!;
            var proxy = (FreeboxParentalApiProxy)parental;
            proxy.Config = Config;
            proxy.Filters = Filters;
            proxy.FiltersException = FiltersException;
            return parental;
        }
    }

    [SuppressMessage(
        "Performance", "CA1852:Seal internal types",
        Justification = "DispatchProxy derives a runtime-generated proxy from this base type.")]
    private class FreeboxParentalApiProxy : DispatchProxy
    {
        public ParentalFilterConfig Config { get; set; } = new();
        public ParentalFilter[] Filters { get; set; } = [];
        public Exception? FiltersException { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            return targetMethod?.Name switch
            {
                nameof(IFreeboxParentalApi.GetConfigAsync) => Task.FromResult(Config),
                nameof(IFreeboxParentalApi.GetFiltersAsync) => FiltersException is null
                    ? Task.FromResult(Filters)
                    : throw FiltersException,
                _ => throw new InvalidOperationException("Unexpected parental API member invocation.")
            };
        }
    }
}
