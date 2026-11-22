using Microsoft.Extensions.Logging.Abstractions;

namespace Freenaute.Freebox.Mcp.Tools.Shares;

[McpServerToolType]
public sealed partial class FreeboxSharesTools(
    IFreeboxOrchestrator orchestrator,
    ILogger<FreeboxSharesTools>? logger = null)
{
    private const int MaxInterfaces = 32;
    private const int MaxHostsPerInterface = 128;

    private readonly ILogger<FreeboxSharesTools> _logger =
        logger ?? NullLogger<FreeboxSharesTools>.Instance;

    [McpServerTool(Name = "freebox_nas_visibility", Title = "Get Freebox NAS visibility", ReadOnly = true,
        Idempotent = true, Destructive = false, OpenWorld = true, UseStructuredContent = true,
        OutputSchemaType = typeof(NasVisibilityToolResult))]
    [Description("Returns bounded sanitized NAS/storage visibility state without exposing identifiers.")]
    public async Task<NasVisibilityToolResult> GetNasVisibilityAsync(CancellationToken cancellationToken)
    {
        LogToolStarted(_logger, "freebox_nas_visibility");
        FreeboxCoreResult<FreeboxSystemSnapshot> result = await orchestrator
            .GetSystemSnapshotAsync(cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            LogToolFailed(_logger, "freebox_nas_visibility", result.Error?.Code);
            return ToolResultFactory.Failure<NasVisibilityToolResult>(result.Error!);
        }

        FreeboxSystemSnapshot value = result.Value!;
        LogToolSucceeded(_logger, "freebox_nas_visibility", 1);
        return new NasVisibilityToolResult(
            value.UserStoragePowered,
            Safe.Text(value.DiskStatus, 32),
            Safe.Text(value.FirmwareVersion, 32),
            Safe.Text(value.Model, 64));
    }

    [McpServerTool(Name = "freebox_shares_visibility", Title = "Get Freebox shares visibility", ReadOnly = true,
        Idempotent = true, Destructive = false, OpenWorld = true, UseStructuredContent = true,
        OutputSchemaType = typeof(SharesVisibilityToolResult))]
    [Description(
        "Returns bounded LAN share visibility. Interface names are sanitized and host identifiers are not exposed.")]
    public async Task<SharesVisibilityToolResult> GetSharesVisibilityAsync(
        bool includeHosts = false,
        string? interfaceName = null,
        CancellationToken cancellationToken = default)
    {
        LogToolStarted(_logger, "freebox_shares_visibility");
        if (interfaceName is not null && (interfaceName.Length is < 1 or > 64 || interfaceName.Any(char.IsControl)))
        {
            throw new ArgumentException("interfaceName must be 1 to 64 characters and contain no control characters.",
                nameof(interfaceName));
        }

        FreeboxCoreResult<FreeboxNetworkSnapshot> result = await orchestrator
            .GetNetworkSnapshotAsync(includeHosts, interfaceName, cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            LogToolFailed(_logger, "freebox_shares_visibility", result.Error?.Code);
            return ToolResultFactory.Failure<SharesVisibilityToolResult>(result.Error!);
        }

        FreeboxLanSnapshot lan = result.Value!.Lan;
        int totalReachable = 0;
        int totalActive = 0;
        bool hasHostCounters = false;

        ShareInterfaceVisibilityToolResult[] interfaces =
        [
            .. lan.Interfaces
                .Where(item =>
                    interfaceName is null || string.Equals(item.Name, interfaceName, StringComparison.Ordinal))
                .Take(MaxInterfaces)
                .Select(item =>
                {
                    int? reachable = null;
                    int? active = null;
                    if (includeHosts && lan.Hosts.TryGetValue(item.Name ?? string.Empty,
                            out FreeboxLanHostSnapshot[]? rawHosts))
                    {
                        FreeboxLanHostSnapshot[] hosts = [.. rawHosts.Take(MaxHostsPerInterface)];
                        reachable = hosts.Count(host => host.Reachable == true);
                        active = hosts.Count(host => host.Active == true);
                        totalReachable += reachable.Value;
                        totalActive += active.Value;
                        hasHostCounters = true;
                    }

                    return new ShareInterfaceVisibilityToolResult(
                        Safe.Text(item.Name, 64),
                        item.HostCount,
                        reachable,
                        active);
                })
        ];

        LogToolSucceeded(_logger, "freebox_shares_visibility", interfaces.Length);
        return new SharesVisibilityToolResult(
            Safe.Text(lan.Name, 64),
            Safe.Text(lan.Type, 32),
            Safe.Text(lan.Mode, 32),
            interfaces.Length,
            hasHostCounters ? totalReachable : null,
            hasHostCounters ? totalActive : null,
            interfaces);
    }

    [LoggerMessage(EventId = 1800, Level = LogLevel.Information, Message = "Executing {ToolName}.")]
    private static partial void LogToolStarted(ILogger logger, string toolName);

    [LoggerMessage(EventId = 1801, Level = LogLevel.Warning, Message = "{ToolName} failed: {ErrorCode}")]
    private static partial void LogToolFailed(ILogger logger, string toolName, FreeboxCoreErrorCode? errorCode);

    [LoggerMessage(EventId = 1802, Level = LogLevel.Information,
        Message = "{ToolName} succeeded (countHint={CountHint}).")]
    private static partial void LogToolSucceeded(ILogger logger, string toolName, int countHint);
}
