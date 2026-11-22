namespace Freenaute.Freebox.Mcp.Tools.Tests.Downloads;

public sealed class DownloadToolMappingTests
{
    [Fact]
    public void MapDownload_SanitizesOutputAndExcludesSensitiveFields()
    {
        var download = new Download
        {
            Id = 42,
            Name = "A\0B",
            Type = "bt",
            Status = "downloading",
            Size = 100,
            RxBytes = 25,
            RxRate = 10,
            ArchivePassword = "secret",
            InfoHash = "hash"
        };

        DownloadToolResult result = FreeboxDownloadsTools.MapDownload(download);

        Assert.Equal("AB", result.Name);
        Assert.Equal("bt", result.Type);
        Assert.Equal("downloading", result.Status);
        Assert.Equal(100, result.Size);
        Assert.Equal(25, result.RxBytes);
        Assert.Equal(10, result.RxRate);
        Assert.NotEqual("42", result.Id);
        Assert.DoesNotContain("secret", result.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain("hash", result.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetAsync_InvalidId_ReturnsInvalidRequestError()
    {
        var tools = new FreeboxDownloadsTools(null!);

        InvalidOperationException exception =
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                tools.GetAsync(0, TestContext.Current.CancellationToken));

        Assert.Contains("InvalidRequest", exception.Message, StringComparison.Ordinal);
    }
}
