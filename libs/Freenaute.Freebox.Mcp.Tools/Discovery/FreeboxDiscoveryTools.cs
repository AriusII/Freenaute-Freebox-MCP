using Microsoft.Extensions.Logging.Abstractions;

namespace Freenaute.Freebox.Mcp.Tools.Discovery;

[McpServerToolType]
public sealed partial class FreeboxDiscoveryTools(
    IFreeboxOrchestrator orchestrator,
    ILogger<FreeboxDiscoveryTools>? logger = null)
{
    private readonly ILogger<FreeboxDiscoveryTools> _logger =
        logger ?? NullLogger<FreeboxDiscoveryTools>.Instance;

    [McpServerTool(Name = "freebox_discover", Title = "Discover Freebox", ReadOnly = true, Idempotent = true,
        Destructive = false, OpenWorld = true, UseStructuredContent = true,
        OutputSchemaType = typeof(DiscoveryToolResult))]
    [Description("Discovers the reachable Freebox and returns bounded, non-identifying API capability metadata.")]
    public async Task<DiscoveryToolResult> DiscoverAsync(CancellationToken cancellationToken)
    {
        LogToolStarted(_logger);
        FreeboxCoreResult<FreeboxDiscoverySnapshot> result = await orchestrator.DiscoverAsync(cancellationToken)
            .ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            LogToolFailed(_logger, result.Error?.Code);
            return ToolResultFactory.Failure<DiscoveryToolResult>(result.Error!);
        }

        FreeboxDiscoverySnapshot value = result.Value!;
        LogToolSucceeded(_logger, value.HttpsAvailable, value.HttpsPort);
        return new DiscoveryToolResult(
            Safe.Text(value.BoxModel, 64),
            Safe.Text(value.ApiVersion, 32),
            value.HttpsAvailable,
            value.HttpsPort);
    }

    [LoggerMessage(EventId = 1000, Level = LogLevel.Information, Message = "Executing freebox_discover.")]
    private static partial void LogToolStarted(ILogger logger);

    [LoggerMessage(EventId = 1001, Level = LogLevel.Warning, Message = "freebox_discover failed: {ErrorCode}")]
    private static partial void LogToolFailed(ILogger logger, FreeboxCoreErrorCode? errorCode);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Information,
        Message = "freebox_discover succeeded (httpsAvailable={HttpsAvailable}, httpsPort={HttpsPort}).")]
    private static partial void LogToolSucceeded(ILogger logger, bool? httpsAvailable, int? httpsPort);
}
