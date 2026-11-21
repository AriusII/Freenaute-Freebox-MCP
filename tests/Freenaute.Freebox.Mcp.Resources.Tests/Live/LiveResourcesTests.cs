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

    private sealed class StubSource(string content, string? mediaType) : IFreeboxLiveResourceSource
    {
        public ValueTask<FreeboxLiveSnapshot> GetSnapshotAsync(CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(new FreeboxLiveSnapshot(content, mediaType));
        }
    }
}
