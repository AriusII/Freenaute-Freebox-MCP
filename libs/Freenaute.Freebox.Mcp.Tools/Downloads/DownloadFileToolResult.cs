namespace Freenaute.Freebox.Mcp.Tools.Downloads;

public sealed record DownloadFileToolResult(
    string Id,
    string Name,
    string MimeType,
    long? Size,
    long? ReceivedBytes,
    string Status,
    string Error,
    string Priority);
