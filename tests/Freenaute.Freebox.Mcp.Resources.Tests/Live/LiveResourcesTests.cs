namespace Freenaute.Freebox.Mcp.Resources.Tests.Live;

public sealed class LiveResourcesTests
{
    [Fact]
    public async Task GetStatusAsync_WithoutSource_ReturnsSafeFallback()
    {
        TextResourceContents result = await new LiveResources().GetStatusAsync(TestContext.Current.CancellationToken);
        Assert.Equal("freebox://live/status", result.Uri);
        Assert.Equal("text/markdown", result.MimeType);
        Assert.Contains("no `IFreeboxLiveResourceSource` is registered", result.Text, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(" application/json ", "application/json")]
    [InlineData("", "text/plain")]
    [InlineData("x\r\ny", "text/plain")]
    public async Task GetStatusAsync_NormalizesSourceMediaType(string mediaType, string expected)
    {
        var resource = new LiveResources(new StubSource("ok", mediaType));
        TextResourceContents result = await resource.GetStatusAsync(TestContext.Current.CancellationToken);
        Assert.Equal(expected, result.MimeType);
    }

    [Fact]
    public async Task GetStatusAsync_TruncatedSource_AppendsMarker()
    {
        var resource = new LiveResources(new StubSource(new string('a', 64 * 1024 + 1), null));
        TextResourceContents result = await resource.GetStatusAsync(TestContext.Current.CancellationToken);
        Assert.EndsWith("[resource content truncated after 64 KiB]\n", result.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void GetStatusAsync_HasExpectedResourceMetadata()
    {
        MethodInfo method = typeof(LiveResources).GetMethod(nameof(LiveResources.GetStatusAsync))!;
        McpServerResourceAttribute metadata = Assert.Single(method.GetCustomAttributes<McpServerResourceAttribute>());
        Assert.Equal("freebox://live/status", metadata.UriTemplate);
        Assert.Equal("freebox-live-status", metadata.Name);
        Assert.Contains("sanitized", metadata.Title, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("text/markdown", metadata.MimeType);
    }

    [Theory]
    [InlineData(nameof(LiveResources.GetNetworkAdvancedAsync), "freebox://live/network-advanced", "freebox-live-network-advanced")]
    [InlineData(nameof(LiveResources.GetFilesAdvancedAsync), "freebox://live/files-advanced", "freebox-live-files-advanced")]
    [InlineData(nameof(LiveResources.GetSystemHomeControlsAsync), "freebox://live/systemhome-controls", "freebox-live-systemhome-controls")]
    [InlineData(nameof(LiveResources.GetServicesControlsAsync), "freebox://live/services-controls", "freebox-live-services-controls")]
    [InlineData(nameof(LiveResources.GetParentalWriteAsync), "freebox://live/parental-write", "freebox-live-parental-write")]
    public void DomainLiveResource_HasExpectedMetadata(string methodName, string uri, string name)
    {
        MethodInfo method = typeof(LiveResources).GetMethod(methodName)!;
        McpServerResourceAttribute metadata = Assert.Single(method.GetCustomAttributes<McpServerResourceAttribute>());
        Assert.Equal(uri, metadata.UriTemplate);
        Assert.Equal(name, metadata.Name);
        Assert.Equal("text/markdown", metadata.MimeType);
    }

    [Theory]
    [InlineData(nameof(LiveResources.GetNetworkAdvancedAsync), "freebox://live/network-advanced")]
    [InlineData(nameof(LiveResources.GetFilesAdvancedAsync), "freebox://live/files-advanced")]
    [InlineData(nameof(LiveResources.GetSystemHomeControlsAsync), "freebox://live/systemhome-controls")]
    [InlineData(nameof(LiveResources.GetServicesControlsAsync), "freebox://live/services-controls")]
    [InlineData(nameof(LiveResources.GetParentalWriteAsync), "freebox://live/parental-write")]
    public async Task DomainLiveResource_WithoutSource_ReturnsSafeFallback(string methodName, string uri)
    {
        var resource = new LiveResources();
        MethodInfo method = typeof(LiveResources).GetMethod(methodName)!;
        TextResourceContents result = await Assert.IsType<ValueTask<TextResourceContents>>(
            method.Invoke(resource, [TestContext.Current.CancellationToken]));
        Assert.Equal(uri, result.Uri);
        Assert.Equal("text/markdown", result.MimeType);
        Assert.Contains("Live data is unavailable", result.Text, StringComparison.Ordinal);
    }

    private sealed class StubSource(string content, string? mediaType) : IFreeboxLiveResourceSource
    {
        public ValueTask<FreeboxLiveSnapshot> GetSnapshotAsync(CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(new FreeboxLiveSnapshot(content, mediaType));
        }
    }
}
