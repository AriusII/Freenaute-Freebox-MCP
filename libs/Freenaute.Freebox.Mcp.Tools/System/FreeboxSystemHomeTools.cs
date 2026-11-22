using Microsoft.Extensions.Logging.Abstractions;

namespace Freenaute.Freebox.Mcp.Tools.System;

[McpServerToolType]
public sealed partial class FreeboxSystemHomeTools
{
    private const int MaxPlanningModes = 8;
    private const int MaxMappingSlots = 10_080;
    private const int MaxPlanningModeLength = 32;
    private readonly Func<CancellationToken, Task<UpdateStatus>> _getUpdatesAsync;
    private readonly Func<CancellationToken, Task<StandbyStatus>> _getStandbyStatusAsync;
    private readonly Func<CancellationToken, Task<StandbyConfig>> _getStandbyConfigAsync;
    private readonly Func<StandbyConfigUpdate, CancellationToken, Task<StandbyConfig>> _configureStandbyAsync;
    private readonly ILogger<FreeboxSystemHomeTools> _logger;

    public FreeboxSystemHomeTools(
        IFreeboxClient? client,
        Func<CancellationToken, Task<UpdateStatus>>? getUpdatesAsync = null,
        Func<CancellationToken, Task<StandbyStatus>>? getStandbyStatusAsync = null,
        Func<CancellationToken, Task<StandbyConfig>>? getStandbyConfigAsync = null,
        Func<StandbyConfigUpdate, CancellationToken, Task<StandbyConfig>>? configureStandbyAsync = null,
        ILogger<FreeboxSystemHomeTools>? logger = null)
    {
        _logger = logger ?? NullLogger<FreeboxSystemHomeTools>.Instance;
        if (getUpdatesAsync is null ||
            getStandbyStatusAsync is null ||
            getStandbyConfigAsync is null ||
            configureStandbyAsync is null)
        {
            ArgumentNullException.ThrowIfNull(client);
        }

        _getUpdatesAsync = getUpdatesAsync ??
                           (cancellationToken => client!.SystemHome.Updates.GetAsync(cancellationToken));
        _getStandbyStatusAsync = getStandbyStatusAsync ??
                                 (cancellationToken =>
                                     client!.SystemHome.Standby.GetStatusAsync(cancellationToken));
        _getStandbyConfigAsync = getStandbyConfigAsync ??
                                  (cancellationToken =>
                                      client!.SystemHome.Standby.GetConfigurationAsync(cancellationToken));
        _configureStandbyAsync = configureStandbyAsync ??
                                 ((update, cancellationToken) => client!.SystemHome.Standby.Configure()
                                     .UseFields(update)
                                     .SendAsync(cancellationToken));
    }

    [McpServerTool(Name = "freebox_system_updates_status", Title = "Get Freebox system update status",
        ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = true, UseStructuredContent = true,
        OutputSchemaType = typeof(SystemUpdatesStatusToolResult))]
    [Description("Returns bounded update states and firmware version summaries; diagnostic messages are excluded.")]
    public async Task<SystemUpdatesStatusToolResult> GetUpdatesStatusAsync(CancellationToken cancellationToken)
    {
        LogToolStarted(_logger, "freebox_system_updates_status");
        try
        {
            UpdateStatus status = await _getUpdatesAsync(cancellationToken).ConfigureAwait(false);
            return new SystemUpdatesStatusToolResult(
                Safe.Text(status.State?.ToString(), 32),
                Safe.Text(status.UpgradeState?.State?.ToString(), 32),
                status.UpgradeState?.Percent,
                Safe.Text(status.UpgradeState?.OldVersion, 64),
                Safe.Text(status.UpgradeState?.NewVersion, 64));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            LogToolFailed(_logger, "freebox_system_updates_status", exception.Message);
            throw MapFailure(exception);
        }
    }

    [McpServerTool(Name = "freebox_system_standby_status", Title = "Get Freebox standby status",
        ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = true, UseStructuredContent = true,
        OutputSchemaType = typeof(StandbyStatusToolResult))]
    [Description(
        "Returns bounded standby status and aggregate schedule counts; the detailed schedule is excluded.")]
    public async Task<StandbyStatusToolResult> GetStandbyStatusAsync(CancellationToken cancellationToken)
    {
        LogToolStarted(_logger, "freebox_system_standby_status");
        try
        {
            Task<StandbyStatus> statusTask = _getStandbyStatusAsync(cancellationToken);
            Task<StandbyConfig> configTask = _getStandbyConfigAsync(cancellationToken);
            await Task.WhenAll(statusTask, configTask).ConfigureAwait(false);

            StandbyStatus status = await statusTask.ConfigureAwait(false);
            StandbyConfig config = await configTask.ConfigureAwait(false);
            ImmutableArray<bool> mapping = config.Mapping ?? [];
            return new StandbyStatusToolResult(
                config.UsePlanning ?? status.UsePlanning,
                Safe.Text((config.PlanningMode ?? status.PlanningMode)?.ToString(), MaxPlanningModeLength),
                ToBoundedPlanningModes(status.AvailablePlanningModes),
                status.NextChange,
                config.Resolution,
                mapping.Length,
                mapping.Count(enabled => enabled));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            LogToolFailed(_logger, "freebox_system_standby_status", exception.Message);
            throw MapFailure(exception);
        }
    }

    [McpServerTool(Name = "freebox_system_standby_configure", Title = "Configure Freebox standby",
        ReadOnly = false, Idempotent = true, Destructive = false, OpenWorld = true, UseStructuredContent = true,
        OutputSchemaType = typeof(StandbyConfigurationToolResult))]
    [Description("Updates only supplied standby settings and returns aggregate configuration status without the schedule.")]
    public async Task<StandbyConfigurationToolResult> ConfigureStandbyAsync(
        [Description("Optional standby planning enabled state.")] bool? usePlanning = null,
        [Description("Optional bounded standby planning mode (up to 32 characters).")] string? planningMode = null,
        [Description("Optional weekly standby schedule (at most 10,080 slots).")] bool[]? mapping = null,
        CancellationToken cancellationToken = default)
    {
        LogToolStarted(_logger, "freebox_system_standby_configure");
        ValidateConfigureArguments(usePlanning, planningMode, mapping);

        StandbyConfigUpdate update = new();
        if (usePlanning.HasValue)
        {
            update = update.WithUsePlanning(usePlanning.Value);
        }

        if (planningMode is not null)
        {
            update = update.WithPlanningMode(new StandbyPlanningMode(planningMode));
        }

        if (mapping is not null)
        {
            update = update.WithMapping(mapping);
        }

        try
        {
            update.Validate();
            StandbyConfig config = await _configureStandbyAsync(update, cancellationToken).ConfigureAwait(false);
            ImmutableArray<bool> configuredMapping = config.Mapping ?? [];
            return new StandbyConfigurationToolResult(
                config.UsePlanning,
                Safe.Text(config.PlanningMode?.ToString(), MaxPlanningModeLength),
                config.Resolution,
                configuredMapping.Length,
                configuredMapping.Count(enabled => enabled));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            LogToolFailed(_logger, "freebox_system_standby_configure", exception.Message);
            throw MapFailure(exception);
        }
    }

    private static string[] ToBoundedPlanningModes(ImmutableArray<StandbyPlanningMode>? modes)
    {
        return modes is null
            ? []
            :
            [
                .. modes.Value.Take(MaxPlanningModes)
                    .Select(mode => Safe.Text(mode.ToString(), MaxPlanningModeLength))
                    .Where(mode => !string.IsNullOrWhiteSpace(mode))
            ];
    }

    private static void ValidateConfigureArguments(bool? usePlanning, string? planningMode, bool[]? mapping)
    {
        if (!usePlanning.HasValue && planningMode is null && mapping is null)
        {
            throw new InvalidOperationException("InvalidRequest: at least one standby setting must be supplied.");
        }

        if (planningMode is not null &&
            (string.IsNullOrWhiteSpace(planningMode) ||
             planningMode.Length > MaxPlanningModeLength ||
             Safe.Text(planningMode, MaxPlanningModeLength) != planningMode))
        {
            throw new InvalidOperationException(
                "InvalidRequest: planningMode must be 1 to 32 characters and contain no control characters.");
        }

        if (mapping is { Length: > MaxMappingSlots })
        {
            throw new InvalidOperationException("InvalidRequest: mapping must contain at most 10,080 slots.");
        }
    }

    private static InvalidOperationException MapFailure(Exception exception)
    {
        FreeboxCoreError error = FreeboxErrorMapper.Map(exception);
        return new InvalidOperationException($"{error.Code}: {error.Message}", exception);
    }

    [LoggerMessage(EventId = 2200, Level = LogLevel.Information, Message = "Executing {ToolName}.")]
    private static partial void LogToolStarted(ILogger logger, string toolName);

    [LoggerMessage(EventId = 2201, Level = LogLevel.Warning, Message = "{ToolName} failed: {Reason}")]
    private static partial void LogToolFailed(ILogger logger, string toolName, string? reason);
}
