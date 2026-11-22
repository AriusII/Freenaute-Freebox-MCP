namespace Freenaute.Freebox.Mcp.Tools.Tests.Files;

public sealed class FreeboxFilesToolsTests
{
    [Theory]
    [InlineData(nameof(FreeboxFilesTools.ListAsync), "freebox_files_list", typeof(FilesListToolResult), true, false)]
    [InlineData(nameof(FreeboxFilesTools.GetInfoAsync), "freebox_files_info", typeof(FileInfoToolResult), true, false)]
    [InlineData(nameof(FreeboxFilesTools.GetTasksAsync), "freebox_files_tasks", typeof(FilesTasksToolResult), true, false)]
    [InlineData(nameof(FreeboxFilesTools.CreateDirectoryAsync), "freebox_files_mkdir", typeof(FilesMutationToolResult), false, false)]
    [InlineData(nameof(FreeboxFilesTools.MoveAsync), "freebox_files_move", typeof(FileTaskToolResult), false, false)]
    [InlineData(nameof(FreeboxFilesTools.DeleteAsync), "freebox_files_delete", typeof(FileTaskToolResult), false, true)]
    public void ToolMethod_HasExpectedMetadata(
        string methodName,
        string toolName,
        Type outputType,
        bool readOnly,
        bool destructive)
    {
        MethodInfo method = typeof(FreeboxFilesTools).GetMethod(methodName)!;
        McpServerToolAttribute tool = Assert.Single(method.GetCustomAttributes<McpServerToolAttribute>());
        DescriptionAttribute description = Assert.Single(method.GetCustomAttributes<DescriptionAttribute>());

        Assert.Equal(toolName, tool.Name);
        Assert.Equal(readOnly, tool.ReadOnly);
        Assert.True(tool.Idempotent);
        Assert.Equal(destructive, tool.Destructive);
        Assert.True(tool.OpenWorld);
        Assert.True(tool.UseStructuredContent);
        Assert.Equal(outputType, tool.OutputSchemaType);
        Assert.False(string.IsNullOrWhiteSpace(description.Description));
    }

    [Fact]
    public async Task DeleteAsync_WithTraversalPath_RejectsInputBeforeCallingApi()
    {
        var tools = new FreeboxFilesTools(null!);
        string[] paths =
        [
            "/Disque 1/../secret"
        ];

        InvalidOperationException exception =
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                tools.DeleteAsync(paths, confirm: true, TestContext.Current.CancellationToken));

        Assert.Contains("InvalidRequest", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DeleteAsync_RequiresExplicitConfirmation()
    {
        var tools = new FreeboxFilesTools(null!);
        string[] paths =
        [
            "/Disque 1/file.txt"
        ];

        InvalidOperationException exception =
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                tools.DeleteAsync(paths, confirm: false, TestContext.Current.CancellationToken));

        Assert.Contains("confirm must be true", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void MapTask_SanitizesOutputAndExcludesPaths()
    {
        var task = new FsTask
        {
            Id = 42,
            Type = "move",
            State = "running",
            Error = "A\0B",
            From = "/secret/from",
            To = "/secret/to",
            Src = ["/secret/a"],
            Dst = "/secret/destination"
        };

        FileTaskToolResult result = FreeboxFilesTools.MapTask(task);

        Assert.Equal("AB", result.Error);
        Assert.NotEqual("42", result.Id);
        Assert.DoesNotContain("secret", result.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void ToolType_IsMcpServerToolType()
    {
        Assert.NotEmpty(typeof(FreeboxFilesTools).GetCustomAttributes<McpServerToolTypeAttribute>());
    }
}
