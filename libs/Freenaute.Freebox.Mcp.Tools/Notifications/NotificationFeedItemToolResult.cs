namespace Freenaute.Freebox.Mcp.Tools.Notifications;

public sealed record NotificationFeedItemToolResult(
    string NotificationId,
    string Severity,
    string Code,
    string Message);
