namespace Freenaute.Freebox.Mcp.Tools.Files;

public sealed record FilesListToolResult(FileEntryToolResult[] Entries, string? Cursor);

public sealed record FileEntryToolResult(
    string Name,
    string Type,
    string Mimetype,
    long? Size,
    long? Modification,
    bool? Link,
    bool? Hidden,
    long? FolderCount,
    long? FileCount);

public sealed record FileInfoToolResult(
    string Name,
    string Type,
    string Mimetype,
    long? Size,
    long? Modification,
    bool? Link,
    bool? Hidden,
    long? FolderCount,
    long? FileCount);

public sealed record FilesTasksToolResult(FileTaskToolResult[] Tasks);

public sealed record FileTaskToolResult(
    string Id,
    string Type,
    string State,
    string Error,
    long? CreatedTs,
    long? StartedTs,
    long? DoneTs,
    long? Progress,
    long? Eta,
    long? Nfiles,
    long? NfilesDone,
    long? TotalBytes,
    long? TotalBytesDone,
    long? Rate);

public sealed record FilesMutationToolResult(bool Success);
