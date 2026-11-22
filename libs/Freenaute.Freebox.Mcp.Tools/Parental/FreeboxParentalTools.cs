namespace Freenaute.Freebox.Mcp.Tools.Parental;

[McpServerToolType]
public sealed class FreeboxParentalTools(IFreeboxClient client)
{
    private const int MaxFilters = 128;

    [McpServerTool(Name = "freebox_parental_config", Title = "Get Freebox parental-control configuration",
        ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = true, UseStructuredContent = true,
        OutputSchemaType = typeof(ParentalConfigToolResult))]
    [Description("Returns sanitized parental-control policy configuration without exposing target identities.")]
    public async Task<ParentalConfigToolResult> GetConfigAsync(CancellationToken cancellationToken)
    {
        try
        {
            ParentalFilterConfig config = await client.Parental.GetConfigAsync(cancellationToken).ConfigureAwait(false);
            return new ParentalConfigToolResult(Safe.Text(config.DefaultFilterMode?.Value, 24));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
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
            return new ParentalFiltersToolResult(filters.Length, mapped);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
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
}
