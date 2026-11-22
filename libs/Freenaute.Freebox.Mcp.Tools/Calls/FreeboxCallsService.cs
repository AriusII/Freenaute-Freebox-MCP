namespace Freenaute.Freebox.Mcp.Tools.Calls;

public sealed class FreeboxCallsService(IFreeboxClient client) : IFreeboxCallsService
{
    private readonly IFreeboxClient _client = client ?? throw new ArgumentNullException(nameof(client));

    public async ValueTask<FreeboxCoreResult<FreeboxCallEntrySnapshot[]>> GetLogAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            CallEntry[] entries = await _client.Services.Call.GetLogAsync(cancellationToken).ConfigureAwait(false);
            return FreeboxCoreResults.Success(entries.Select(MapEntry).ToArray());
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            return FreeboxCoreResults.Failure<FreeboxCallEntrySnapshot[]>(MapError(exception));
        }
    }

    public async ValueTask<FreeboxCoreResult<FreeboxCallAccountSnapshot?>> GetAccountAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            CallAccount account = await _client.Services.Call.GetAccountAsync(cancellationToken).ConfigureAwait(false);
            return FreeboxCoreResults.Success<FreeboxCallAccountSnapshot?>(new FreeboxCallAccountSnapshot(
                account.PhoneNumber));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            return FreeboxCoreResults.Failure<FreeboxCallAccountSnapshot?>(MapError(exception));
        }
    }

    public async ValueTask<FreeboxCoreResult<bool>> MarkAllLogEntriesAsReadAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _client.Services.Call.MarkAllLogEntriesAsReadAsync(cancellationToken).ConfigureAwait(false);
            return FreeboxCoreResults.Success(true);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            return FreeboxCoreResults.Failure<bool>(MapError(exception));
        }
    }

    public async ValueTask<FreeboxCoreResult<bool>> DeleteAllLogEntriesAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _client.Services.Call.DeleteAllLogEntriesAsync(cancellationToken).ConfigureAwait(false);
            return FreeboxCoreResults.Success(true);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            return FreeboxCoreResults.Failure<bool>(MapError(exception));
        }
    }

    private static FreeboxCallEntrySnapshot MapEntry(CallEntry entry)
    {
        return new FreeboxCallEntrySnapshot(
            entry.Id,
            entry.Type?.Value,
            entry.Datetime,
            entry.Duration,
            entry.New,
            entry.ContactId,
            entry.LineId);
    }

    private static FreeboxCoreError MapError(Exception exception)
    {
        return new FreeboxCoreError(
            FreeboxCoreErrorCode.TransportFailure,
            Safe.Text(exception.Message, 256));
    }
}
