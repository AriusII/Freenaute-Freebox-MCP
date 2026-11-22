namespace Freenaute.Freebox.Mcp.Tools.Notifications;

public sealed record StatusFeedEntryToolResult(
    string EntryId,
    string Topic,
    string Level,
    string Summary);
