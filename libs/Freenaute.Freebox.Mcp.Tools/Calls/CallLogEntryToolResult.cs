namespace Freenaute.Freebox.Mcp.Tools.Calls;

public sealed record CallLogEntryToolResult(
    string EntryId,
    string Type,
    long? Timestamp,
    long? DurationSeconds,
    bool? IsNew,
    string? ContactId,
    string? LineId);
