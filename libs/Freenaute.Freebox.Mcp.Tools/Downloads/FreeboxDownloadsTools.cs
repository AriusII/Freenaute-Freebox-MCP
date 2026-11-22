namespace Freenaute.Freebox.Mcp.Tools.Downloads;

[McpServerToolType]
public sealed class FreeboxDownloadsTools(IFreeboxClient client)
{
    private const int MaxDownloads = 128;
    private const int MaxFiles = 512;
    private const int MaxTextLength = 4096;

    [McpServerTool(Name = "freebox_downloads_list", Title = "List Freebox downloads", ReadOnly = true,
        Idempotent = true, Destructive = false, OpenWorld = true, UseStructuredContent = true,
        OutputSchemaType = typeof(DownloadsToolResult))]
    [Description(
        "Returns a bounded sanitized list of Freebox download tasks; paths, hashes, credentials, and passwords are excluded.")]
    public async Task<DownloadsToolResult> ListAsync(CancellationToken cancellationToken)
    {
        try
        {
            ObjectOrArray<Download> result =
                await client.Files.Downloads.GetAllAsync(cancellationToken).ConfigureAwait(false);
            IReadOnlyList<Download> downloads = result.IsArray
                ? result.Items
                : result.IsInitialized
                    ? [result.Object]
                    : Array.Empty<Download>();

            DownloadToolResult[] mapped = [.. downloads.Take(MaxDownloads).Select(MapDownload)];
            return new DownloadsToolResult(mapped);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw MapFailure(exception);
        }
    }

    [McpServerTool(Name = "freebox_download_status", Title = "Get Freebox download status", ReadOnly = true,
        Idempotent = true, Destructive = false, OpenWorld = true, UseStructuredContent = true,
        OutputSchemaType = typeof(DownloadToolResult))]
    [Description(
        "Returns one bounded sanitized Freebox download task; paths, hashes, credentials, and passwords are excluded.")]
    public async Task<DownloadToolResult> GetAsync(
        [Description("Freebox numeric download task ID.")]
        long id,
        CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            throw new InvalidOperationException("InvalidRequest: id must be > 0.");
        }

        try
        {
            Download download = await client.Files.Downloads.GetAsync(id, cancellationToken).ConfigureAwait(false);
            return MapDownload(download);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw MapFailure(exception);
        }
    }

    [McpServerTool(Name = "freebox_download_stats", Title = "Get Freebox download statistics", ReadOnly = true,
        Idempotent = true, Destructive = false, OpenWorld = true, UseStructuredContent = true,
        OutputSchemaType = typeof(DownloadStatsToolResult))]
    [Description("Returns aggregate Freebox download and RSS task counts and transfer rates.")]
    public async Task<DownloadStatsToolResult> GetStatsAsync(CancellationToken cancellationToken)
    {
        try
        {
            DownloadStats stats =
                await client.Files.Downloads.GetStatisticsAsync(cancellationToken).ConfigureAwait(false);
            return new DownloadStatsToolResult(
                stats.NbTasks, stats.NbTasksStopped, stats.NbTasksChecking, stats.NbTasksQueued,
                stats.NbTasksExtracting, stats.NbTasksDone, stats.NbTasksRepairing, stats.NbTasksSeeding,
                stats.NbTasksDownloading, stats.NbTasksError, stats.NbTasksStopping, stats.NbTasksActive,
                stats.NbRss, stats.NbRssItemsUnread, stats.RxRate, stats.TxRate,
                Safe.Text(stats.ThrottlingMode, 32), stats.ThrottlingIsScheduled,
                stats.NbPeer, stats.BlocklistEntries, stats.BlocklistHits);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw MapFailure(exception);
        }
    }

    [McpServerTool(Name = "freebox_download_configuration", Title = "Get Freebox download configuration",
        ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = true, UseStructuredContent = true,
        OutputSchemaType = typeof(DownloadConfigurationToolResult))]
    [Description(
        "Returns bounded sanitized download configuration state; watch and download directories are excluded.")]
    public async Task<DownloadConfigurationToolResult> GetConfigurationAsync(CancellationToken cancellationToken)
    {
        try
        {
            DownloadConfiguration configuration =
                await client.Files.Downloads.GetConfigurationAsync(cancellationToken).ConfigureAwait(false);
            return new DownloadConfigurationToolResult(
                configuration.MaxDownloadingTasks,
                configuration.UseWatchDir,
                Safe.Text(configuration.Throttling?.Mode, 32),
                Safe.Text(configuration.Dns1, 64),
                Safe.Text(configuration.Dns2, 64));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw MapFailure(exception);
        }
    }

    [McpServerTool(Name = "freebox_download_files", Title = "List files in a Freebox download", ReadOnly = true,
        Idempotent = true, Destructive = false, OpenWorld = true, UseStructuredContent = true,
        OutputSchemaType = typeof(DownloadFilesToolResult))]
    [Description(
        "Returns a bounded sanitized file list for one download task; file paths and preview URLs are excluded.")]
    public async Task<DownloadFilesToolResult> GetFilesAsync(
        [Description("Freebox numeric download task ID.")]
        long id,
        CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            throw new InvalidOperationException("InvalidRequest: id must be > 0.");
        }

        try
        {
            DownloadFile[] files =
                await client.Files.Downloads.GetFilesAsync(id, cancellationToken).ConfigureAwait(false);
            DownloadFileToolResult[] mapped =
            [
                .. files.Take(MaxFiles).Select(file =>
                    new DownloadFileToolResult(
                        Safe.StableIdentifier(file.Id),
                        Safe.Text(file.Name, 256),
                        Safe.Text(file.Mimetype, 64),
                        file.Size,
                        file.Rx,
                        Safe.Text(file.Status, 32),
                        Safe.Text(file.Error, 128),
                        Safe.Text(file.Priority, 32)))
            ];
            return new DownloadFilesToolResult(mapped);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw MapFailure(exception);
        }
    }

    [McpServerTool(Name = "freebox_download_log", Title = "Get Freebox download log", ReadOnly = true,
        Idempotent = true, Destructive = false, OpenWorld = true, UseStructuredContent = true,
        OutputSchemaType = typeof(DownloadLogToolResult))]
    [Description("Returns a bounded sanitized log for one Freebox download task.")]
    public async Task<DownloadLogToolResult> GetLogAsync(
        [Description("Freebox numeric download task ID.")]
        long id,
        CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            throw new InvalidOperationException("InvalidRequest: id must be > 0.");
        }

        try
        {
            string log = await client.Files.Downloads.GetLogAsync(id, cancellationToken).ConfigureAwait(false);
            return new DownloadLogToolResult(Safe.Text(log, MaxTextLength));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw MapFailure(exception);
        }
    }

    [McpServerTool(Name = "freebox_download_pieces", Title = "Get Freebox download pieces", ReadOnly = true,
        Idempotent = true, Destructive = false, OpenWorld = true, UseStructuredContent = true,
        OutputSchemaType = typeof(DownloadPiecesToolResult))]
    [Description("Returns bounded piece-state text for one Freebox download task.")]
    public async Task<DownloadPiecesToolResult> GetPiecesAsync(
        [Description("Freebox numeric download task ID.")]
        long id,
        CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            throw new InvalidOperationException("InvalidRequest: id must be > 0.");
        }

        try
        {
            string pieces = await client.Files.Downloads.GetPiecesAsync(id, cancellationToken).ConfigureAwait(false);
            return new DownloadPiecesToolResult(Safe.Text(pieces, MaxTextLength));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw MapFailure(exception);
        }
    }

    public static DownloadToolResult MapDownload(Download download)
    {
        return new DownloadToolResult(
            download.Id.HasValue
                ? Safe.StableIdentifier(download.Id.Value.ToString(CultureInfo.InvariantCulture))
                : "unknown",
            Safe.Text(download.Name, 256),
            Safe.Text(download.Type, 32),
            Safe.Text(download.Status, 32),
            download.Size,
            download.QueuePos,
            Safe.Text(download.IoPriority, 32),
            download.TxBytes,
            download.RxBytes,
            download.TxRate,
            download.RxRate,
            download.TxPct,
            download.RxPct,
            Safe.Text(download.Error, 128),
            download.CreatedTs,
            download.Eta);
    }

    private static InvalidOperationException MapFailure(Exception exception)
    {
        FreeboxCoreError error = new(
            FreeboxCoreErrorCode.TransportFailure,
            Safe.Text(exception.Message, 256));
        return new InvalidOperationException(
            $"{error.Code}: {error.Message}",
            exception);
    }
}
