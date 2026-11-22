namespace Freenaute.Freebox.Mcp.Tools.Notifications;

public sealed record NotificationFeedToolResult(
    DateTimeOffset GeneratedAt,
    NotificationFeedItemToolResult[] Notifications);
