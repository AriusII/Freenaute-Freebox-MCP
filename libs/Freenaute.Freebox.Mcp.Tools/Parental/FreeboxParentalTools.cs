using Microsoft.Extensions.Logging.Abstractions;

namespace Freenaute.Freebox.Mcp.Tools.Parental;

[McpServerToolType]
public sealed partial class FreeboxParentalTools(
    IFreeboxClient client,
    ILogger<FreeboxParentalTools>? logger = null)
{
    private const int MaxFilters = 128;

    private readonly ILogger<FreeboxParentalTools> _logger =
        logger ?? NullLogger<FreeboxParentalTools>.Instance;

    [McpServerTool(Name = "freebox_parental_config", Title = "Get Freebox parental-control configuration",
        ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = true, UseStructuredContent = true,
        OutputSchemaType = typeof(ParentalConfigToolResult))]
    [Description("Returns sanitized parental-control policy configuration without exposing target identities.")]
    public async Task<ParentalConfigToolResult> GetConfigAsync(CancellationToken cancellationToken)
    {
        LogToolStarted(_logger, "freebox_parental_config");
        try
        {
            ParentalFilterConfig config = await client.Parental.GetConfigAsync(cancellationToken).ConfigureAwait(false);
            LogToolSucceeded(_logger, "freebox_parental_config", 1);
            return new ParentalConfigToolResult(Safe.Text(config.DefaultFilterMode?.Value, 24));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            LogToolFailed(_logger, "freebox_parental_config", exception.Message);
            return ToolResultFactory.Failure<ParentalConfigToolResult>(FreeboxErrorMapper.Map(exception));
        }
    }

    [McpServerTool(Name = "freebox_parental_filters", Title = "Get Freebox parental-control filters", ReadOnly = true,
        Idempotent = true, Destructive = false, OpenWorld = true, UseStructuredContent = true,
        OutputSchemaType = typeof(ParentalFiltersToolResult))]
    [Description(
        "Returns bounded sanitized parental filters. MACs, host names, IP addresses, and descriptions are excluded.")]
    public async Task<ParentalFiltersToolResult> GetFiltersAsync(
        [Description("Maximum number of filters to return (1 to 128). Defaults to 64.")]
        int limit = 64,
        CancellationToken cancellationToken = default)
    {
        LogToolStarted(_logger, "freebox_parental_filters");
        if (limit is < 1 or > MaxFilters)
        {
            return ToolResultFactory.Failure<ParentalFiltersToolResult>(new FreeboxCoreError(
                FreeboxCoreErrorCode.InvalidRequest,
                "limit must be between 1 and 128."));
        }

        try
        {
            ParentalFilter[] filters = await client.Parental.GetFiltersAsync(cancellationToken).ConfigureAwait(false);
            ParentalFilterToolResult[] mapped =
            [
                .. filters
                    .Take(limit)
                    .Select(MapFilter)
            ];
            LogToolSucceeded(_logger, "freebox_parental_filters", mapped.Length);
            return new ParentalFiltersToolResult(filters.Length, mapped);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            LogToolFailed(_logger, "freebox_parental_filters", exception.Message);
            return ToolResultFactory.Failure<ParentalFiltersToolResult>(FreeboxErrorMapper.Map(exception));
        }
    }

    private static ParentalFilterToolResult MapFilter(ParentalFilter filter)
    {
        return new ParentalFilterToolResult(
            Safe.StableIdentifier(filter.Id?.ToString(CultureInfo.InvariantCulture)),
            filter.Macs?.Length ?? 0,
            filter.Hosts?.Length ?? 0,
            !string.IsNullOrWhiteSpace(filter.Ip),
            !string.IsNullOrWhiteSpace(filter.Desc),
            filter.Forced,
            Safe.Text(filter.ForcedMode?.Value, 24),
            filter.TmpModeExpire,
            Safe.Text(filter.TmpMode?.Value, 24),
            Safe.Text(filter.SchedulingMode?.Value, 24),
            Safe.Text(filter.FilterState?.Value, 24),
            filter.CurrentMappingIdx,
            filter.NextChange);
    }

    [LoggerMessage(EventId = 2000, Level = LogLevel.Information, Message = "Executing {ToolName}.")]
    private static partial void LogToolStarted(ILogger logger, string toolName);

    [LoggerMessage(EventId = 2001, Level = LogLevel.Information,
        Message = "{ToolName} succeeded (countHint={CountHint}).")]
    private static partial void LogToolSucceeded(ILogger logger, string toolName, int countHint);

    [LoggerMessage(EventId = 2002, Level = LogLevel.Warning, Message = "{ToolName} failed: {Reason}")]
    private static partial void LogToolFailed(ILogger logger, string toolName, string? reason);
}
