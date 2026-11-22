namespace Freenaute.Freebox.Mcp.Tools.Downloads;

public sealed record DownloadConfigurationToolResult(
    long? MaximumDownloadingTasks,
    bool? UseWatchDirectory,
    string ThrottlingMode,
    string DnsPrimary,
    string DnsSecondary);
