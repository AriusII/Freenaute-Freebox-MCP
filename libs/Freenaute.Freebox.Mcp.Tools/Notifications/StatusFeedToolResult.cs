namespace Freenaute.Freebox.Mcp.Tools.Notifications;

public sealed record StatusFeedToolResult(
    DateTimeOffset GeneratedAt,
    StatusFeedEntryToolResult[] Entries);
