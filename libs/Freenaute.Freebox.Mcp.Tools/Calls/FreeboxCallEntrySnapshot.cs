namespace Freenaute.Freebox.Mcp.Tools.Calls;

public sealed record FreeboxCallEntrySnapshot(
    long? Id,
    string? Type,
    long? Datetime,
    long? Duration,
    bool? IsNew,
    long? ContactId,
    long? LineId);
