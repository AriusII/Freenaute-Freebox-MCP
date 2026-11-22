using Microsoft.Extensions.Logging.Abstractions;

namespace Freenaute.Freebox.Mcp.Tools.Files;

[McpServerToolType]
public sealed partial class FreeboxFilesTools(
    IFreeboxClient client,
    ILogger<FreeboxFilesTools>? logger = null)
{
    private const int MaxEntries = 512;
    private const int MaxTasks = 128;
    private const int MaxPaths = 128;
    private const int MaxPathLength = 4096;
    private const int MaxNameLength = 256;

    private readonly ILogger<FreeboxFilesTools> _logger =
        logger ?? NullLogger<FreeboxFilesTools>.Instance;

    [McpServerTool(Name = "freebox_files_list", Title = "List Freebox files", ReadOnly = true,
        Idempotent = true, Destructive = false, OpenWorld = true, UseStructuredContent = true,
        OutputSchemaType = typeof(FilesListToolResult))]
    [Description("Returns a bounded sanitized file listing for an encoded Freebox path.")]
    public async Task<FilesListToolResult> ListAsync(
        [Description("Encoded or UTF-8 Freebox path.")] string path,
        CancellationToken cancellationToken)
    {
        string decodedPath = ValidatePath(path);
        LogToolStarted(_logger, "freebox_files_list");
        try
        {
            var validated = EncodedFreeboxPath.FromUtf8Path(decodedPath);
            FileListing listing = await client.Files.FileSystem
                .ListAsync(validated, new FileListingOptions { Limit = MaxEntries }, cancellationToken)
                .ConfigureAwait(false);

            FileEntryToolResult[] entries =
            [
                .. (listing.Entries ?? []).Take(MaxEntries).Select(MapEntry)
            ];
            LogToolSucceeded(_logger, "freebox_files_list", entries.Length);
            return new FilesListToolResult(entries, Safe.Text(listing.Cursor, 128));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            LogToolFailed(_logger, "freebox_files_list", exception.Message);
            throw MapFailure(exception);
        }
    }

    [McpServerTool(Name = "freebox_files_info", Title = "Get Freebox file information", ReadOnly = true,
        Idempotent = true, Destructive = false, OpenWorld = true, UseStructuredContent = true,
        OutputSchemaType = typeof(FileInfoToolResult))]
    [Description("Returns bounded sanitized metadata for an encoded Freebox path.")]
    public async Task<FileInfoToolResult> GetInfoAsync(
        [Description("Encoded or UTF-8 Freebox path.")] string path,
        CancellationToken cancellationToken)
    {
        string decodedPath = ValidatePath(path);
        LogToolStarted(_logger, "freebox_files_info");
        try
        {
            var validated = EncodedFreeboxPath.FromUtf8Path(decodedPath);
            FileEntry entry = await client.Files.FileSystem
                .GetInformationAsync(validated, cancellationToken).ConfigureAwait(false);
            LogToolSucceeded(_logger, "freebox_files_info", 1);
            return MapInfo(entry);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            LogToolFailed(_logger, "freebox_files_info", exception.Message);
            throw MapFailure(exception);
        }
    }

    [McpServerTool(Name = "freebox_files_tasks", Title = "List Freebox filesystem tasks", ReadOnly = true,
        Idempotent = true, Destructive = false, OpenWorld = true, UseStructuredContent = true,
        OutputSchemaType = typeof(FilesTasksToolResult))]
    [Description("Returns a bounded sanitized list of Freebox filesystem tasks.")]
    public async Task<FilesTasksToolResult> GetTasksAsync(CancellationToken cancellationToken)
    {
        LogToolStarted(_logger, "freebox_files_tasks");
        try
        {
            FsTask[] tasks = await client.Files.FileSystem.GetTasksAsync(cancellationToken).ConfigureAwait(false);
            FileTaskToolResult[] mapped = [.. tasks.Take(MaxTasks).Select(MapTask)];
            LogToolSucceeded(_logger, "freebox_files_tasks", mapped.Length);
            return new FilesTasksToolResult(mapped);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            LogToolFailed(_logger, "freebox_files_tasks", exception.Message);
            throw MapFailure(exception);
        }
    }

    [McpServerTool(Name = "freebox_files_mkdir", Title = "Create a Freebox directory", ReadOnly = false,
        Idempotent = true, Destructive = false, OpenWorld = true, UseStructuredContent = true,
        OutputSchemaType = typeof(FilesMutationToolResult))]
    [Description("Creates one directory beneath a validated encoded Freebox parent path.")]
    public async Task<FilesMutationToolResult> CreateDirectoryAsync(
        [Description("Encoded or UTF-8 Freebox parent path.")] string parent,
        [Description("Directory name, without path separators.")] string dirname,
        CancellationToken cancellationToken)
    {
        string decodedParent = ValidatePath(parent);
        string safeName = ValidateDirectoryName(dirname);
        LogToolStarted(_logger, "freebox_files_mkdir");
        try
        {
            await client.Files.FileSystem.CreateDirectoryAsync(
                new CreateDirectoryRequest
                {
                    Parent = EncodedFreeboxPath.FromUtf8Path(decodedParent),
                    Dirname = safeName
                },
                cancellationToken).ConfigureAwait(false);
            LogToolSucceeded(_logger, "freebox_files_mkdir", 1);
            return new FilesMutationToolResult(true);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            LogToolFailed(_logger, "freebox_files_mkdir", exception.Message);
            throw MapFailure(exception);
        }
    }

    [McpServerTool(Name = "freebox_files_move", Title = "Move Freebox files", ReadOnly = false,
        Idempotent = true, Destructive = false, OpenWorld = true, UseStructuredContent = true,
        OutputSchemaType = typeof(FileTaskToolResult))]
    [Description("Moves bounded validated source paths to a validated Freebox destination.")]
    public async Task<FileTaskToolResult> MoveAsync(
        [Description("Encoded or UTF-8 source paths.")] string[] paths,
        [Description("Encoded or UTF-8 destination path.")] string destination,
        CancellationToken cancellationToken)
    {
        EncodedFreeboxPath[] validatedPaths = ValidatePaths(paths);
        string decodedDestination = ValidatePath(destination);
        LogToolStarted(_logger, "freebox_files_move");
        try
        {
            FsTask task = await client.Files.FileSystem.MoveAsync(
                new FileTransferRequest
                {
                    Files = validatedPaths,
                    Dst = EncodedFreeboxPath.FromUtf8Path(decodedDestination),
                    Mode = FileConflictMode.Recent
                },
                cancellationToken).ConfigureAwait(false);
            LogToolSucceeded(_logger, "freebox_files_move", 1);
            return MapTask(task);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            LogToolFailed(_logger, "freebox_files_move", exception.Message);
            throw MapFailure(exception);
        }
    }

    [McpServerTool(Name = "freebox_files_delete", Title = "Delete Freebox files", ReadOnly = false,
        Idempotent = true, Destructive = true, OpenWorld = true, UseStructuredContent = true,
        OutputSchemaType = typeof(FileTaskToolResult))]
    [Description("Deletes bounded validated Freebox paths; confirm must explicitly be true.")]
    public async Task<FileTaskToolResult> DeleteAsync(
        [Description("Encoded or UTF-8 source paths.")] string[] paths,
        [Description("Explicit confirmation that deletion is intended.")] bool confirm,
        CancellationToken cancellationToken)
    {
        if (!confirm)
        {
            throw new InvalidOperationException("InvalidRequest: confirm must be true for destructive deletion.");
        }

        EncodedFreeboxPath[] validatedPaths = ValidatePaths(paths);
        LogToolStarted(_logger, "freebox_files_delete");
        try
        {
            FsTask task = await client.Files.FileSystem.RemoveAsync(
                new RemoveFilesRequest { Files = validatedPaths },
                cancellationToken).ConfigureAwait(false);
            LogToolSucceeded(_logger, "freebox_files_delete", 1);
            return MapTask(task);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            LogToolFailed(_logger, "freebox_files_delete", exception.Message);
            throw MapFailure(exception);
        }
    }

    internal static string ValidatePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new InvalidOperationException("InvalidRequest: path must be initialized.");
        }

        string decoded = path;
        try
        {
            decoded = EncodedFreeboxPath.FromEncoded(path).DecodeUtf8Path();
        }
        catch (FormatException)
        {
            // Accept plain UTF-8 path input when the value is not base64-encoded.
        }
        catch (InvalidOperationException)
        {
            // Accept plain UTF-8 path input when the value is not a valid encoded path.
        }

        if (string.IsNullOrWhiteSpace(decoded) || decoded.Length > MaxPathLength)
        {
            throw new InvalidOperationException("InvalidRequest: path is empty or exceeds 4096 characters.");
        }

        if (decoded.Any(char.IsControl))
        {
            throw new InvalidOperationException("InvalidRequest: path contains control characters.");
        }

        foreach (string segment in decoded.Replace('\\', '/').Split('/', StringSplitOptions.None))
        {
            if (segment is "." or "..")
            {
                throw new InvalidOperationException("InvalidRequest: path traversal segments are not allowed.");
            }
        }

        return decoded;
    }

    private static EncodedFreeboxPath[] ValidatePaths(string[]? paths)
    {
        if (paths is null || paths.Length is 0)
        {
            throw new InvalidOperationException("InvalidRequest: at least one source path is required.");
        }

        if (paths.Length > MaxPaths)
        {
            throw new InvalidOperationException("InvalidRequest: no more than 128 source paths are allowed.");
        }

        var validated = new EncodedFreeboxPath[paths.Length];
        for (int index = 0; index < paths.Length; index++)
        {
            string decoded = ValidatePath(paths[index]);
            validated[index] = EncodedFreeboxPath.FromUtf8Path(decoded);
        }

        return validated;
    }

    private static string ValidateDirectoryName(string? dirname)
    {
        if (string.IsNullOrWhiteSpace(dirname) ||
            dirname.Length > MaxNameLength ||
            dirname.Any(char.IsControl) ||
            dirname.Contains('/') ||
            dirname.Contains('\\') ||
            dirname is "." or "..")
        {
            throw new InvalidOperationException(
                "InvalidRequest: dirname must be a single non-empty name of at most 256 characters.");
        }

        return Safe.Text(dirname, MaxNameLength);
    }

    public static FileEntryToolResult MapEntry(FileEntry entry)
    {
        return new FileEntryToolResult(
            Safe.Text(entry.Name, MaxNameLength),
            Safe.Text(entry.Type, 32),
            Safe.Text(entry.Mimetype, 64),
            entry.Size,
            entry.Modification,
            entry.Link,
            entry.Hidden,
            entry.Foldercount,
            entry.Filecount);
    }

    public static FileInfoToolResult MapInfo(FileEntry entry)
    {
        return new FileInfoToolResult(
            Safe.Text(entry.Name, MaxNameLength),
            Safe.Text(entry.Type, 32),
            Safe.Text(entry.Mimetype, 64),
            entry.Size,
            entry.Modification,
            entry.Link,
            entry.Hidden,
            entry.Foldercount,
            entry.Filecount);
    }

    public static FileTaskToolResult MapTask(FsTask task)
    {
        return new FileTaskToolResult(
            task.Id.HasValue
                ? Safe.StableIdentifier(task.Id.Value.ToString(CultureInfo.InvariantCulture))
                : "unknown",
            Safe.Text(task.Type, 32),
            Safe.Text(task.State, 32),
            Safe.Text(task.Error, 128),
            task.CreatedTs,
            task.StartedTs,
            task.DoneTs,
            task.Progress,
            task.Eta,
            task.Nfiles,
            task.NfilesDone,
            task.TotalBytes,
            task.TotalBytesDone,
            task.Rate);
    }

    private static InvalidOperationException MapFailure(Exception exception)
    {
        FreeboxCoreError error = new(
            FreeboxCoreErrorCode.TransportFailure,
            Safe.Text(exception.Message, 256));
        return new InvalidOperationException($"{error.Code}: {error.Message}", exception);
    }

    [LoggerMessage(EventId = 1500, Level = LogLevel.Information, Message = "Executing {ToolName}.")]
    private static partial void LogToolStarted(ILogger logger, string toolName);

    [LoggerMessage(EventId = 1501, Level = LogLevel.Information,
        Message = "Tool {ToolName} succeeded (countHint={CountHint}).")]
    private static partial void LogToolSucceeded(ILogger logger, string toolName, int countHint);

    [LoggerMessage(EventId = 1502, Level = LogLevel.Warning,
        Message = "Tool {ToolName} failed. Reason: {Reason}")]
    private static partial void LogToolFailed(ILogger logger, string toolName, string? reason);
}
