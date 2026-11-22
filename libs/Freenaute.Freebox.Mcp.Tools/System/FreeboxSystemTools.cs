using Microsoft.Extensions.Logging.Abstractions;

namespace Freenaute.Freebox.Mcp.Tools.System;

[McpServerToolType]
public sealed partial class FreeboxSystemTools(
    IFreeboxOrchestrator orchestrator,
    ILogger<FreeboxSystemTools>? logger = null)
{
    private const int MaxComponents = 32;

    private readonly ILogger<FreeboxSystemTools> _logger =
        logger ?? NullLogger<FreeboxSystemTools>.Instance;

    [McpServerTool(Name = "freebox_system_status", Title = "Get Freebox system status", ReadOnly = true,
        Idempotent = true, Destructive = false, OpenWorld = true, UseStructuredContent = true,
        OutputSchemaType = typeof(SystemToolResult))]
    [Description("Returns bounded sanitized firmware, uptime, health, sensor, fan, and expansion status.")]
    public async Task<SystemToolResult> GetStatusAsync(CancellationToken cancellationToken)
    {
        LogSystemStatusStarted(_logger);
        FreeboxCoreResult<FreeboxSystemSnapshot> result = await orchestrator
            .GetSystemSnapshotAsync(cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            LogSystemStatusFailed(_logger, result.Error?.Code);
            return ToolResultFactory.Failure<SystemToolResult>(result.Error!);
        }

        FreeboxSystemSnapshot value = result.Value!;
        LogSystemStatusSucceeded(_logger, value.Sensors.Length, value.Fans.Length, value.Expansions.Length);
        return new SystemToolResult(
            Safe.Text(value.FirmwareVersion, 32),
            Safe.Text(value.Model, 64),
            Safe.Text(value.BoardName, 64),
            value.UptimeSeconds, value.BoxAuthenticated, value.Usb3Enabled, value.UserStoragePowered,
            Safe.Text(value.DiskStatus, 32),
            [
                .. value.Sensors.Take(MaxComponents).Select(item =>
                    new SystemSensorToolResult(Safe.StableIdentifier(item.Id), string.Empty, item.Value))
            ],
            [
                .. value.Fans.Take(MaxComponents).Select(item =>
                    new SystemFanToolResult(Safe.StableIdentifier(item.Id), string.Empty, item.Value))
            ],
            [
                .. value.Expansions.Take(MaxComponents)
                    .Select(item => new SystemExpansionToolResult(item.Slot, item.Present, item.Supported, null,
                        Safe.Text(item.Type, 32)))
            ]);
    }

    [LoggerMessage(EventId = 2100, Level = LogLevel.Information, Message = "Executing freebox_system_status.")]
    private static partial void LogSystemStatusStarted(ILogger logger);

    [LoggerMessage(EventId = 2101, Level = LogLevel.Warning, Message = "freebox_system_status failed: {ErrorCode}")]
    private static partial void LogSystemStatusFailed(ILogger logger, FreeboxCoreErrorCode? errorCode);

    [LoggerMessage(EventId = 2102, Level = LogLevel.Information,
        Message =
            "freebox_system_status succeeded (sensors={SensorCount}, fans={FanCount}, expansions={ExpansionCount}).")]
    private static partial void LogSystemStatusSucceeded(
        ILogger logger,
        int sensorCount,
        int fanCount,
        int expansionCount);
}
