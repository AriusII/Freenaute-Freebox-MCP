namespace Freenaute.Freebox.Mcp.Tools.Cameras;

[McpServerToolType]
public sealed class FreeboxCamerasTools
{
    private const int MaxCameras = 32;
    private const int MaxLanguages = 8;
    private const int MaxPlanningModes = 8;
    private readonly Func<CancellationToken, Task<LanguageSupport>> _getLanguageAsync;
    private readonly Func<CancellationToken, Task<StandbyConfig>> _getStandbyConfigAsync;
    private readonly Func<CancellationToken, Task<StandbyStatus>> _getStandbyStatusAsync;
    private readonly Func<CancellationToken, Task<UpdateStatus>> _getUpdatesAsync;

    private readonly Func<CancellationToken, Task<Camera[]>> _listCamerasAsync;

    public FreeboxCamerasTools(
        IFreeboxClient? client,
        Func<CancellationToken, Task<Camera[]>>? listCamerasAsync = null,
        Func<CancellationToken, Task<LanguageSupport>>? getLanguageAsync = null,
        Func<CancellationToken, Task<StandbyStatus>>? getStandbyStatusAsync = null,
        Func<CancellationToken, Task<StandbyConfig>>? getStandbyConfigAsync = null,
        Func<CancellationToken, Task<UpdateStatus>>? getUpdatesAsync = null)
    {
        if (listCamerasAsync is null ||
            getLanguageAsync is null ||
            getStandbyStatusAsync is null ||
            getStandbyConfigAsync is null ||
            getUpdatesAsync is null)
        {
            ArgumentNullException.ThrowIfNull(client);
        }

        _listCamerasAsync = listCamerasAsync ?? (cancellationToken => client!.Cameras.ListAsync(cancellationToken));
        _getLanguageAsync = getLanguageAsync ??
                            (cancellationToken => client!.SystemHome.Language.GetAsync(cancellationToken));
        _getStandbyStatusAsync = getStandbyStatusAsync ??
                                 (cancellationToken => client!.SystemHome.Standby.GetStatusAsync(cancellationToken));
        _getStandbyConfigAsync = getStandbyConfigAsync ?? (cancellationToken =>
            client!.SystemHome.Standby.GetConfigurationAsync(cancellationToken));
        _getUpdatesAsync = getUpdatesAsync ??
                           (cancellationToken => client!.SystemHome.Updates.GetAsync(cancellationToken));
    }

    [McpServerTool(Name = "freebox_cameras_overview", Title = "Get Freebox cameras overview", ReadOnly = true,
        Idempotent = true, Destructive = false, OpenWorld = true, UseStructuredContent = true,
        OutputSchemaType = typeof(CamerasToolResult))]
    [Description("Returns bounded camera summaries without names, LAN IDs, or stream URLs.")]
    public async Task<CamerasToolResult> GetCamerasAsync(CancellationToken cancellationToken)
    {
        try
        {
            Camera[] cameras = await _listCamerasAsync(cancellationToken).ConfigureAwait(false);
            return new CamerasToolResult([.. cameras.Take(MaxCameras).Select(MapCamera)]);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            return ToolResultFactory.Failure<CamerasToolResult>(FreeboxErrorMapper.Map(exception));
        }
    }

    [McpServerTool(Name = "freebox_home_status", Title = "Get Freebox home status", ReadOnly = true,
        Idempotent = true, Destructive = false, OpenWorld = true, UseStructuredContent = true,
        OutputSchemaType = typeof(HomeStatusToolResult))]
    [Description("Returns bounded home language, standby, and update states without sensitive payloads.")]
    public async Task<HomeStatusToolResult> GetHomeStatusAsync(CancellationToken cancellationToken)
    {
        try
        {
            Task<LanguageSupport> languageTask = _getLanguageAsync(cancellationToken);
            Task<StandbyStatus> standbyStatusTask = _getStandbyStatusAsync(cancellationToken);
            Task<StandbyConfig> standbyConfigTask = _getStandbyConfigAsync(cancellationToken);
            Task<UpdateStatus> updatesTask = _getUpdatesAsync(cancellationToken);

            await Task.WhenAll(languageTask, standbyStatusTask, standbyConfigTask, updatesTask).ConfigureAwait(false);

            LanguageSupport language = await languageTask.ConfigureAwait(false);
            StandbyStatus standbyStatus = await standbyStatusTask.ConfigureAwait(false);
            StandbyConfig standbyConfig = await standbyConfigTask.ConfigureAwait(false);
            UpdateStatus updates = await updatesTask.ConfigureAwait(false);

            return new HomeStatusToolResult(
                Safe.Text(language.Lang, 16),
                ToBoundedTexts(language.AvailableLanguages, MaxLanguages, 16),
                standbyConfig.UsePlanning ?? standbyStatus.UsePlanning,
                Safe.Text((standbyConfig.PlanningMode ?? standbyStatus.PlanningMode)?.ToString(), 32),
                ToBoundedTexts(standbyStatus.AvailablePlanningModes, MaxPlanningModes, 32),
                standbyStatus.NextChange,
                standbyConfig.Resolution,
                Safe.Text(updates.State?.ToString(), 32),
                Safe.Text(updates.UpgradeState?.State?.ToString(), 32),
                updates.UpgradeState?.Percent);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            return ToolResultFactory.Failure<HomeStatusToolResult>(FreeboxErrorMapper.Map(exception));
        }
    }

    private static CameraToolResult MapCamera(Camera camera)
    {
        return new CameraToolResult(
            Safe.StableIdentifier(camera.Id),
            camera.NodeId,
            !string.IsNullOrWhiteSpace(camera.StreamUrl),
            !string.IsNullOrWhiteSpace(camera.LanId));
    }

    private static string[] ToBoundedTexts<T>(ImmutableArray<T>? values, int maxItems, int maxLength)
    {
        if (!values.HasValue)
        {
            return [];
        }

        return
        [
            .. values.Value.Take(maxItems)
                .Select(item => Safe.Text(item is null ? null : item.ToString(), maxLength))
                .Where(item => !string.IsNullOrWhiteSpace(item))
        ];
    }
}
