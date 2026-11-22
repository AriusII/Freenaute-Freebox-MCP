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

    [McpServerTool(Name = "freebox_parental_filter_create", Title = "Create Freebox parental-control filter",
        ReadOnly = false, Idempotent = false, Destructive = false, OpenWorld = true,
        UseStructuredContent = true, OutputSchemaType = typeof(ParentalMutationToolResult))]
    [Description("Creates a bounded parental-control filter without exposing identities in the result.")]
    public async Task<ParentalMutationToolResult> CreateFilterAsync(
        [Description("MAC addresses to associate with the filter (maximum 128).")]
        string[]? macs = null,
        [Description("Short non-sensitive description (maximum 128 characters).")]
        string? description = null,
        [Description("Whether forced mode is enabled.")]
        bool? forced = null,
        [Description("Forced mode: allowed, denied, or webonly.")]
        string? forcedMode = null,
        [Description("Temporary mode expiration Unix timestamp.")]
        long? tmpModeExpire = null,
        [Description("Temporary mode: allowed, denied, or webonly.")]
        string? tmpMode = null,
        CancellationToken cancellationToken = default)
    {
        const string toolName = "freebox_parental_filter_create";
        LogToolStarted(_logger, toolName);
        ParentalFilterWriteRequest? request = BuildFilterRequest(
            macs, description, forced, forcedMode, tmpModeExpire, tmpMode);
        if (request is null)
        {
            return InvalidMutation(toolName, "invalid parental filter request.");
        }

        try
        {
            ParentalFilter result = await client.Parental.CreateFilterAsync(request, cancellationToken).ConfigureAwait(false);
            string stableId = Safe.StableIdentifier(result.Id?.ToString(CultureInfo.InvariantCulture));
            LogToolSucceeded(_logger, toolName, 1);
            return new ParentalMutationToolResult(true, "Parental filter created.", stableId);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            LogToolFailed(_logger, toolName, exception.Message);
            return ToolResultFactory.Failure<ParentalMutationToolResult>(FreeboxErrorMapper.Map(exception));
        }
    }

    [McpServerTool(Name = "freebox_parental_filter_update", Title = "Update Freebox parental-control filter",
        ReadOnly = false, Idempotent = true, Destructive = false, OpenWorld = true,
        UseStructuredContent = true, OutputSchemaType = typeof(ParentalMutationToolResult))]
    [Description("Updates one bounded parental-control filter without returning target identities.")]
    public async Task<ParentalMutationToolResult> UpdateFilterAsync(
        [Description("Stable numeric parental filter identifier.")]
        long filterId,
        [Description("MAC addresses to associate with the filter (maximum 128).")]
        string[]? macs = null,
        [Description("Short non-sensitive description (maximum 128 characters).")]
        string? description = null,
        [Description("Whether forced mode is enabled.")]
        bool? forced = null,
        [Description("Forced mode: allowed, denied, or webonly.")]
        string? forcedMode = null,
        [Description("Temporary mode expiration Unix timestamp.")]
        long? tmpModeExpire = null,
        [Description("Temporary mode: allowed, denied, or webonly.")]
        string? tmpMode = null,
        CancellationToken cancellationToken = default)
    {
        const string toolName = "freebox_parental_filter_update";
        LogToolStarted(_logger, toolName);
        if (filterId <= 0)
        {
            return InvalidMutation(toolName, "filterId must be a positive identifier.");
        }
        ParentalFilterWriteRequest? request = BuildFilterRequest(
            macs, description, forced, forcedMode, tmpModeExpire, tmpMode);
        if (request is null)
        {
            return InvalidMutation(toolName, "invalid parental filter request.");
        }

        try
        {
            ParentalFilter result = await client.Parental.Filter(filterId)
                .UpdateAsync(request, cancellationToken).ConfigureAwait(false);
            string stableId = Safe.StableIdentifier(result.Id?.ToString(CultureInfo.InvariantCulture));
            LogToolSucceeded(_logger, toolName, 1);
            return new ParentalMutationToolResult(true, "Parental filter updated.", stableId);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            LogToolFailed(_logger, toolName, exception.Message);
            return ToolResultFactory.Failure<ParentalMutationToolResult>(FreeboxErrorMapper.Map(exception));
        }
    }

    [McpServerTool(Name = "freebox_parental_filter_delete", Title = "Delete Freebox parental-control filter",
        ReadOnly = false, Idempotent = true, Destructive = true, OpenWorld = true,
        UseStructuredContent = true, OutputSchemaType = typeof(ParentalMutationToolResult))]
    [Description("Deletes one parental-control filter. Requires confirm=true.")]
    public async Task<ParentalMutationToolResult> DeleteFilterAsync(
        [Description("Stable numeric parental filter identifier.")]
        long filterId,
        [Description("Explicit confirmation required to perform deletion.")]
        bool confirm,
        CancellationToken cancellationToken = default)
    {
        const string toolName = "freebox_parental_filter_delete";
        LogToolStarted(_logger, toolName);
        if (!confirm)
        {
            return InvalidMutation(toolName, "Deletion was not performed because confirm was false.");
        }
        if (filterId <= 0)
        {
            return InvalidMutation(toolName, "filterId must be a positive identifier.");
        }

        try
        {
            await client.Parental.Filter(filterId).DeleteAsync(cancellationToken).ConfigureAwait(false);
            LogToolSucceeded(_logger, toolName, 1);
            return new ParentalMutationToolResult(true, "Parental filter deleted.");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            LogToolFailed(_logger, toolName, exception.Message);
            return ToolResultFactory.Failure<ParentalMutationToolResult>(FreeboxErrorMapper.Map(exception));
        }
    }

    [McpServerTool(Name = "freebox_parental_filter_planning_update",
        Title = "Update Freebox parental-filter planning", ReadOnly = false, Idempotent = true,
        Destructive = false, OpenWorld = true, UseStructuredContent = true,
        OutputSchemaType = typeof(ParentalMutationToolResult))]
    [Description("Updates bounded time-planning settings for one parental-control filter.")]
    public async Task<ParentalMutationToolResult> UpdatePlanningAsync(
        [Description("Stable numeric parental filter identifier.")]
        long filterId,
        [Description("Planning resolution in minutes.")]
        long? resolution = null,
        [Description("Calendar day ranges (maximum 32 entries).")]
        string[]? cdayranges = null,
        [Description("Planning state sequence (maximum 10080 entries).")]
        string[]? mapping = null,
        CancellationToken cancellationToken = default)
    {
        const string toolName = "freebox_parental_filter_planning_update";
        LogToolStarted(_logger, toolName);
        if (filterId <= 0)
        {
            return InvalidMutation(toolName, "filterId must be a positive identifier.");
        }
        if (cdayranges?.Length > 32 || mapping?.Length > 10080)
        {
            return InvalidMutation(toolName, "planning input exceeds the bounded limits.");
        }

        Optional<ParentalFilterState[]> mappingOptional = default;
        if (mapping is not null)
        {
            if (mapping.Length > 10080 ||
                mapping.Any(value => ParseFilterState(value) is null))
            {
                return InvalidMutation(toolName, "planning mapping must contain allowed, denied, or webonly.");
            }
            mappingOptional = Optional<ParentalFilterState[]>.FromValue(
                [.. mapping.Select(value => ParseFilterState(value)!.Value)]);
        }
        ParentalFilterPlanningPatch patch = new()
        {
            Resolution = resolution is null
                ? default
                : Optional<long>.FromValue(resolution.Value),
            Cdayranges = cdayranges is null
                ? default
                : Optional<string[]>.FromValue([.. cdayranges.Take(32)]),
            Mapping = mappingOptional
        };

        try
        {
            await client.Parental.Filter(filterId).Planning
                .UpdateAsync(patch, cancellationToken).ConfigureAwait(false);
            LogToolSucceeded(_logger, toolName, 1);
            return new ParentalMutationToolResult(true, "Parental filter planning updated.");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            LogToolFailed(_logger, toolName, exception.Message);
            return ToolResultFactory.Failure<ParentalMutationToolResult>(FreeboxErrorMapper.Map(exception));
        }
    }

    [McpServerTool(Name = "freebox_parental_config_update", Title = "Update Freebox parental configuration",
        ReadOnly = false, Idempotent = true, Destructive = false, OpenWorld = true,
        UseStructuredContent = true, OutputSchemaType = typeof(ParentalMutationToolResult))]
    [Description("Updates the default parental-control filter mode.")]
    public async Task<ParentalMutationToolResult> UpdateConfigAsync(
        [Description("Default mode: allowed, denied, or webonly.")]
        string defaultFilterMode,
        CancellationToken cancellationToken = default)
    {
        const string toolName = "freebox_parental_config_update";
        LogToolStarted(_logger, toolName);
        ParentalFilterState? mode = ParseFilterState(defaultFilterMode);
        if (mode is null)
        {
            return InvalidMutation(toolName, "defaultFilterMode must be allowed, denied, or webonly.");
        }

        try
        {
            ParentalFilterConfigPatch patch = new()
            {
                DefaultFilterMode = Optional<ParentalFilterState>.FromValue(mode.Value)
            };
            await client.Parental.UpdateConfigAsync(patch, cancellationToken).ConfigureAwait(false);
            LogToolSucceeded(_logger, toolName, 1);
            return new ParentalMutationToolResult(true, "Parental configuration updated.");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            LogToolFailed(_logger, toolName, exception.Message);
            return ToolResultFactory.Failure<ParentalMutationToolResult>(FreeboxErrorMapper.Map(exception));
        }
    }

    private static ParentalFilterWriteRequest? BuildFilterRequest(
        string[]? macs,
        string? description,
        bool? forced,
        string? forcedMode,
        long? tmpModeExpire,
        string? tmpMode)
    {
        if (macs?.Length > 128 ||
            macs?.Any(string.IsNullOrWhiteSpace) == true ||
            description?.Length > 128)
        {
            return null;
        }
        if (tmpModeExpire is < 0)
        {
            return null;
        }

        ParentalFilterState? parsedForcedMode = forcedMode is null ? null : ParseFilterState(forcedMode);
        if (forcedMode is not null && parsedForcedMode is null)
        {
            return null;
        }
        ParentalFilterState? parsedTmpMode = tmpMode is null ? null : ParseFilterState(tmpMode);
        if (tmpMode is not null && parsedTmpMode is null)
        {
            return null;
        }

        ParentalFilterWriteRequest request = new()
        {
            Macs = macs is null ? default : Optional<string[]>.FromValue([.. macs]),
            Desc = description is null ? default : Optional<string>.FromValue(description),
            Forced = forced is null ? default : Optional<bool>.FromValue(forced.Value),
            ForcedMode = parsedForcedMode is null
                ? default
                : Optional<ParentalFilterState>.FromValue(parsedForcedMode.Value),
            TmpModeExpire = tmpModeExpire is null
                ? default
                : Optional<long>.FromValue(tmpModeExpire.Value),
            TmpMode = parsedTmpMode is null
                ? default
                : Optional<ParentalFilterState>.FromValue(parsedTmpMode.Value)
        };
        return request;
    }

    private static ParentalFilterState? ParseFilterState(string? value)
    {
        return value?.Trim().ToLowerInvariant() switch
        {
            "allowed" => ParentalFilterState.Allowed,
            "denied" => ParentalFilterState.Denied,
            "webonly" => ParentalFilterState.Webonly,
            _ => null
        };
    }

    private ParentalMutationToolResult InvalidMutation(string toolName, string reason)
    {
        LogToolFailed(_logger, toolName, reason);
        return ToolResultFactory.Failure<ParentalMutationToolResult>(
            new FreeboxCoreError(FreeboxCoreErrorCode.InvalidRequest, reason));
    }

    private static ParentalFilterToolResult MapFilter(ParentalFilter filter)
    {
#pragma warning disable CS0618
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
#pragma warning restore CS0618
    }

    [LoggerMessage(EventId = 2000, Level = LogLevel.Information, Message = "Executing {ToolName}.")]
    private static partial void LogToolStarted(ILogger logger, string toolName);

    [LoggerMessage(EventId = 2001, Level = LogLevel.Information,
        Message = "{ToolName} succeeded (countHint={CountHint}).")]
    private static partial void LogToolSucceeded(ILogger logger, string toolName, int countHint);

    [LoggerMessage(EventId = 2002, Level = LogLevel.Warning, Message = "{ToolName} failed: {Reason}")]
    private static partial void LogToolFailed(ILogger logger, string toolName, string? reason);
}
