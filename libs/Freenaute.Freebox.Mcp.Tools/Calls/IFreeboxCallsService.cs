namespace Freenaute.Freebox.Mcp.Tools.Calls;

public interface IFreeboxCallsService
{
    ValueTask<FreeboxCoreResult<FreeboxCallEntrySnapshot[]>> GetLogAsync(
        CancellationToken cancellationToken = default);

    ValueTask<FreeboxCoreResult<FreeboxCallAccountSnapshot?>> GetAccountAsync(
        CancellationToken cancellationToken = default);

    ValueTask<FreeboxCoreResult<bool>> MarkAllLogEntriesAsReadAsync(
        CancellationToken cancellationToken = default);

    ValueTask<FreeboxCoreResult<bool>> DeleteAllLogEntriesAsync(
        CancellationToken cancellationToken = default);
}
