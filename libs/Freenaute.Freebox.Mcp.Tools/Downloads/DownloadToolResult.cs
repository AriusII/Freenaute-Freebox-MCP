namespace Freenaute.Freebox.Mcp.Tools.Downloads;

public sealed record DownloadToolResult(
    string Id,
    string Name,
    string Type,
    string Status,
    long? Size,
    long? QueuePosition,
    string IoPriority,
    long? TxBytes,
    long? RxBytes,
    long? TxRate,
    long? RxRate,
    long? TxPercent,
    long? RxPercent,
    string Error,
    long? CreatedTimestamp,
    long? Eta);
