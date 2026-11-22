namespace Freenaute.Freebox.Mcp.Tools.Downloads;

public sealed record DownloadStatsToolResult(
    long? TotalTasks,
    long? StoppedTasks,
    long? CheckingTasks,
    long? QueuedTasks,
    long? ExtractingTasks,
    long? DoneTasks,
    long? RepairingTasks,
    long? SeedingTasks,
    long? DownloadingTasks,
    long? ErrorTasks,
    long? StoppingTasks,
    long? ActiveTasks,
    long? RssFeeds,
    long? UnreadRssItems,
    long? RxRate,
    long? TxRate,
    string ThrottlingMode,
    bool? ThrottlingIsScheduled,
    long? PeerCount,
    long? BlocklistEntries,
    long? BlocklistHits);
