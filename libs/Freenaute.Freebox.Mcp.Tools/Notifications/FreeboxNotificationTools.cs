using Microsoft.Extensions.Logging.Abstractions;

namespace Freenaute.Freebox.Mcp.Tools.Notifications;

[McpServerToolType]
public sealed partial class FreeboxNotificationTools(
    IFreeboxOrchestrator orchestrator,
    ILogger<FreeboxNotificationTools>? logger = null)
{
    private const int MaxStatusEntries = 8;
    private const int MaxNotifications = 16;

    private readonly ILogger<FreeboxNotificationTools> _logger =
        logger ?? NullLogger<FreeboxNotificationTools>.Instance;

    [McpServerTool(Name = "freebox_status_feed", Title = "Get Freebox status feed", ReadOnly = true,
        Idempotent = true, Destructive = false, OpenWorld = true, UseStructuredContent = true,
        OutputSchemaType = typeof(StatusFeedToolResult))]
    [Description("Returns a bounded, sanitized status feed for pairing, connectivity, Wi-Fi, and system health.")]
    public async Task<StatusFeedToolResult> GetStatusFeedAsync(CancellationToken cancellationToken)
    {
        LogToolStarted(_logger, "freebox_status_feed");
        Task<FreeboxCoreResult<FreeboxConnectionResult>> connectionTask =
            orchestrator.GetConnectionStatusAsync(cancellationToken).AsTask();
        Task<FreeboxCoreResult<FreeboxNetworkSnapshot>> networkTask =
            orchestrator.GetNetworkSnapshotAsync(cancellationToken: cancellationToken).AsTask();
        Task<FreeboxCoreResult<FreeboxSystemSnapshot>> systemTask =
            orchestrator.GetSystemSnapshotAsync(cancellationToken).AsTask();

        await Task.WhenAll(connectionTask, networkTask, systemTask).ConfigureAwait(false);

        FreeboxCoreResult<FreeboxConnectionResult> connectionResult = await connectionTask.ConfigureAwait(false);
        if (!connectionResult.IsSuccess)
        {
            LogToolFailed(_logger, "freebox_status_feed", connectionResult.Error?.Code);
            return ToolResultFactory.Failure<StatusFeedToolResult>(connectionResult.Error!);
        }

        FreeboxCoreResult<FreeboxNetworkSnapshot> networkResult = await networkTask.ConfigureAwait(false);
        if (!networkResult.IsSuccess)
        {
            LogToolFailed(_logger, "freebox_status_feed", networkResult.Error?.Code);
            return ToolResultFactory.Failure<StatusFeedToolResult>(networkResult.Error!);
        }

        FreeboxCoreResult<FreeboxSystemSnapshot> systemResult = await systemTask.ConfigureAwait(false);
        if (!systemResult.IsSuccess)
        {
            LogToolFailed(_logger, "freebox_status_feed", systemResult.Error?.Code);
            return ToolResultFactory.Failure<StatusFeedToolResult>(systemResult.Error!);
        }

        FreeboxConnectionResult connection = connectionResult.Value!;
        FreeboxNetworkSnapshot network = networkResult.Value!;
        FreeboxSystemSnapshot system = systemResult.Value!;

        StatusFeedEntryToolResult[] entries =
        [
            Entry("pairing", MapCredentialLevel(connection.CredentialState),
                $"credential state={Safe.Text(connection.CredentialState.ToString(), 32)}"),
            Entry("connection", "info",
                $"state={Safe.Text(network.Connection.State, 32)}; media={Safe.Text(network.Connection.Media, 32)}"),
            Entry("lan", "info",
                $"interfaces={network.Lan.Interfaces.Length}; hosts={CountHosts(network.Lan.Hosts)}"),
            Entry("wifi", network.Wifi.Enabled == true ? "info" : "warning",
                $"enabled={network.Wifi.Enabled}; state={Safe.Text(network.Wifi.State, 32)}"),
            Entry("system", MapSystemLevel(system),
                $"disk={Safe.Text(system.DiskStatus, 32)}; uptime_seconds={system.UptimeSeconds}")
        ];

        LogToolSucceeded(_logger, "freebox_status_feed", entries.Length);
        return new StatusFeedToolResult(
            DateTimeOffset.UtcNow,
            [.. entries.Take(MaxStatusEntries)]);
    }

    [McpServerTool(Name = "freebox_notifications_feed", Title = "Get Freebox notifications feed", ReadOnly = true,
        Idempotent = true, Destructive = false, OpenWorld = true, UseStructuredContent = true,
        OutputSchemaType = typeof(NotificationFeedToolResult))]
    [Description("Returns actionable bounded notifications derived from current sanitized Freebox status.")]
    public async Task<NotificationFeedToolResult> GetNotificationsFeedAsync(CancellationToken cancellationToken)
    {
        LogToolStarted(_logger, "freebox_notifications_feed");
        Task<FreeboxCoreResult<FreeboxConnectionResult>> connectionTask =
            orchestrator.GetConnectionStatusAsync(cancellationToken).AsTask();
        Task<FreeboxCoreResult<FreeboxNetworkSnapshot>> networkTask =
            orchestrator.GetNetworkSnapshotAsync(cancellationToken: cancellationToken).AsTask();
        Task<FreeboxCoreResult<FreeboxSystemSnapshot>> systemTask =
            orchestrator.GetSystemSnapshotAsync(cancellationToken).AsTask();

        await Task.WhenAll(connectionTask, networkTask, systemTask).ConfigureAwait(false);

        FreeboxCoreResult<FreeboxConnectionResult> connectionResult = await connectionTask.ConfigureAwait(false);
        if (!connectionResult.IsSuccess)
        {
            LogToolFailed(_logger, "freebox_notifications_feed", connectionResult.Error?.Code);
            return ToolResultFactory.Failure<NotificationFeedToolResult>(connectionResult.Error!);
        }

        FreeboxCoreResult<FreeboxNetworkSnapshot> networkResult = await networkTask.ConfigureAwait(false);
        if (!networkResult.IsSuccess)
        {
            LogToolFailed(_logger, "freebox_notifications_feed", networkResult.Error?.Code);
            return ToolResultFactory.Failure<NotificationFeedToolResult>(networkResult.Error!);
        }

        FreeboxCoreResult<FreeboxSystemSnapshot> systemResult = await systemTask.ConfigureAwait(false);
        if (!systemResult.IsSuccess)
        {
            LogToolFailed(_logger, "freebox_notifications_feed", systemResult.Error?.Code);
            return ToolResultFactory.Failure<NotificationFeedToolResult>(systemResult.Error!);
        }

        FreeboxConnectionResult connection = connectionResult.Value!;
        FreeboxNetworkSnapshot network = networkResult.Value!;
        FreeboxSystemSnapshot system = systemResult.Value!;

        var notifications = new List<NotificationFeedItemToolResult>(MaxNotifications);

        if (connection.CredentialState is not FreeboxCredentialState.Approved)
        {
            notifications.Add(Notification("credential_state", "warning", "CREDENTIAL_STATE",
                $"Credential state is {Safe.Text(connection.CredentialState.ToString(), 32)}."));
        }

        if (!IsConnected(network.Connection.State))
        {
            notifications.Add(Notification("connection_state", "warning", "CONNECTION_STATE",
                $"Connection state is {Safe.Text(network.Connection.State, 32)}."));
        }

        if (network.Wifi.Enabled != true)
        {
            notifications.Add(Notification("wifi_disabled", "info", "WIFI_DISABLED",
                "Wi-Fi is disabled."));
        }

        if (system.BoxAuthenticated == false)
        {
            notifications.Add(Notification("box_unauthenticated", "warning", "BOX_UNAUTHENTICATED",
                "Box authentication is disabled."));
        }

        if (!IsHealthyDiskStatus(system.DiskStatus))
        {
            notifications.Add(Notification("disk_status", "warning", "DISK_STATUS",
                $"Disk status is {Safe.Text(system.DiskStatus, 32)}."));
        }

        if (notifications.Count == 0)
        {
            notifications.Add(Notification("all_clear", "info", "ALL_CLEAR", "No actionable notifications."));
        }

        LogToolSucceeded(_logger, "freebox_notifications_feed", notifications.Count);
        return new NotificationFeedToolResult(
            DateTimeOffset.UtcNow,
            [.. notifications.Take(MaxNotifications)]);
    }

    private static StatusFeedEntryToolResult Entry(string topic, string level, string summary)
    {
        return new StatusFeedEntryToolResult(
            Safe.StableIdentifier(topic),
            Safe.Text(topic, 32),
            Safe.Text(level, 16),
            Safe.Text(summary, 160));
    }

    private static NotificationFeedItemToolResult Notification(string key, string severity, string code, string message)
    {
        return new NotificationFeedItemToolResult(
            Safe.StableIdentifier(key),
            Safe.Text(severity, 16),
            Safe.Text(code, 32),
            Safe.Text(message, 160));
    }

    private static string MapCredentialLevel(FreeboxCredentialState state)
    {
        return state == FreeboxCredentialState.Approved ? "info" : "warning";
    }

    private static string MapSystemLevel(FreeboxSystemSnapshot snapshot)
    {
        return IsHealthyDiskStatus(snapshot.DiskStatus) && snapshot.BoxAuthenticated != false ? "info" : "warning";
    }

    private static bool IsConnected(string? state)
    {
        return string.Equals(state, "up", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(state, "connected", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsHealthyDiskStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return true;
        }

        return status.Equals("healthy", StringComparison.OrdinalIgnoreCase) ||
               status.Equals("ok", StringComparison.OrdinalIgnoreCase) ||
               status.Equals("good", StringComparison.OrdinalIgnoreCase) ||
               status.Equals("ready", StringComparison.OrdinalIgnoreCase) ||
               status.Equals("active", StringComparison.OrdinalIgnoreCase);
    }

    private static int CountHosts(IReadOnlyDictionary<string, FreeboxLanHostSnapshot[]> hosts)
    {
        return hosts.Values.Sum(items => items.Length);
    }

    [LoggerMessage(EventId = 1900, Level = LogLevel.Information, Message = "Executing {ToolName}.")]
    private static partial void LogToolStarted(ILogger logger, string toolName);

    [LoggerMessage(EventId = 1901, Level = LogLevel.Warning, Message = "{ToolName} failed: {ErrorCode}")]
    private static partial void LogToolFailed(ILogger logger, string toolName, FreeboxCoreErrorCode? errorCode);

    [LoggerMessage(EventId = 1902, Level = LogLevel.Information,
        Message = "{ToolName} succeeded (itemCount={ItemCount}).")]
    private static partial void LogToolSucceeded(ILogger logger, string toolName, int itemCount);
}
