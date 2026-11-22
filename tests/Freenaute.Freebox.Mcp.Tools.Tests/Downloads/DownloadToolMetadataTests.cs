namespace Freenaute.Freebox.Mcp.Tools.Tests.Downloads;

public sealed class DownloadToolMetadataTests
{
    [Theory]
    [InlineData(nameof(FreeboxDownloadsTools.ListAsync), "freebox_downloads_list", typeof(DownloadsToolResult))]
    [InlineData(nameof(FreeboxDownloadsTools.GetAsync), "freebox_download_status", typeof(DownloadToolResult))]
    [InlineData(nameof(FreeboxDownloadsTools.GetStatsAsync), "freebox_download_stats", typeof(DownloadStatsToolResult))]
    [InlineData(nameof(FreeboxDownloadsTools.GetConfigurationAsync), "freebox_download_configuration",
        typeof(DownloadConfigurationToolResult))]
    [InlineData(nameof(FreeboxDownloadsTools.GetFilesAsync), "freebox_download_files", typeof(DownloadFilesToolResult))]
    [InlineData(nameof(FreeboxDownloadsTools.GetLogAsync), "freebox_download_log", typeof(DownloadLogToolResult))]
    [InlineData(nameof(FreeboxDownloadsTools.GetPiecesAsync), "freebox_download_pieces",
        typeof(DownloadPiecesToolResult))]
    public void ToolMethod_HasReadOnlyMetadata(string methodName, string toolName, Type outputType)
    {
        MethodInfo method = typeof(FreeboxDownloadsTools).GetMethod(methodName)!;
        McpServerToolAttribute tool = Assert.Single(method.GetCustomAttributes<McpServerToolAttribute>());
        DescriptionAttribute description = Assert.Single(method.GetCustomAttributes<DescriptionAttribute>());

        Assert.Equal(toolName, tool.Name);
        Assert.True(tool.ReadOnly);
        Assert.True(tool.Idempotent);
        Assert.False(tool.Destructive);
        Assert.True(tool.OpenWorld);
        Assert.True(tool.UseStructuredContent);
        Assert.Equal(outputType, tool.OutputSchemaType);
        Assert.False(string.IsNullOrWhiteSpace(description.Description));
    }

    [Fact]
    public void ToolType_IsMcpServerToolType()
    {
        Assert.NotEmpty(typeof(FreeboxDownloadsTools).GetCustomAttributes<McpServerToolTypeAttribute>());
    }
}
