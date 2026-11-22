namespace Freenaute.Freebox.Mcp.Tools.System;

[McpServerToolType]
public sealed class FreeboxSystemTools(IFreeboxOrchestrator orchestrator)
{
    private const int MaxComponents = 32;

    [McpServerTool(Name = "freebox_system_status", Title = "Get Freebox system status", ReadOnly = true,
        Idempotent = true, Destructive = false, OpenWorld = true, UseStructuredContent = true,
        OutputSchemaType = typeof(SystemToolResult))]
    [Description("Returns bounded sanitized firmware, uptime, health, sensor, fan, and expansion status.")]
    public async Task<SystemToolResult> GetStatusAsync(CancellationToken cancellationToken)
    {
        FreeboxCoreResult<FreeboxSystemSnapshot> result = await orchestrator
            .GetSystemSnapshotAsync(cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return ToolResultFactory.Failure<SystemToolResult>(result.Error!);
        }

        FreeboxSystemSnapshot value = result.Value!;
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
}
