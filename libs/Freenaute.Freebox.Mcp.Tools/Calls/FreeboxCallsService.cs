using Microsoft.Extensions.Logging.Abstractions;

namespace Freenaute.Freebox.Mcp.Tools.Calls;

public sealed partial class FreeboxCallsService(
    IFreeboxClient client,
    ILogger<FreeboxCallsService>? logger = null) : IFreeboxCallsService
{
    private readonly IFreeboxClient _client = client ?? throw new ArgumentNullException(nameof(client));

    private readonly ILogger<FreeboxCallsService> _logger =
        logger ?? NullLogger<FreeboxCallsService>.Instance;

    public async ValueTask<FreeboxCoreResult<FreeboxCallEntrySnapshot[]>> GetLogAsync(
        CancellationToken cancellationToken = default)
    {
        LogOperationStarted(_logger, "GetLog");
        try
        {
            CallEntry[] entries = await _client.Services.Call.GetLogAsync(cancellationToken).ConfigureAwait(false);
            LogOperationSucceeded(_logger, "GetLog", entries.Length);
            return FreeboxCoreResults.Success(entries.Select(MapEntry).ToArray());
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            LogOperationFailed(_logger, "GetLog", exception.Message);
            return FreeboxCoreResults.Failure<FreeboxCallEntrySnapshot[]>(MapError(exception));
        }
    }

    public async ValueTask<FreeboxCoreResult<FreeboxCallAccountSnapshot?>> GetAccountAsync(
        CancellationToken cancellationToken = default)
    {
        LogOperationStarted(_logger, "GetAccount");
        try
        {
            CallAccount account = await _client.Services.Call.GetAccountAsync(cancellationToken).ConfigureAwait(false);
            LogOperationSucceeded(_logger, "GetAccount", account.PhoneNumber is null ? 0 : 1);
            return FreeboxCoreResults.Success<FreeboxCallAccountSnapshot?>(new FreeboxCallAccountSnapshot(
                account.PhoneNumber));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            LogOperationFailed(_logger, "GetAccount", exception.Message);
            return FreeboxCoreResults.Failure<FreeboxCallAccountSnapshot?>(MapError(exception));
        }
    }

    public async ValueTask<FreeboxCoreResult<bool>> MarkAllLogEntriesAsReadAsync(
        CancellationToken cancellationToken = default)
    {
        LogOperationStarted(_logger, "MarkAllLogEntriesAsRead");
        try
        {
            await _client.Services.Call.MarkAllLogEntriesAsReadAsync(cancellationToken).ConfigureAwait(false);
            LogMutationSucceeded(_logger, "MarkAllLogEntriesAsRead");
            return FreeboxCoreResults.Success(true);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            LogOperationFailed(_logger, "MarkAllLogEntriesAsRead", exception.Message);
            return FreeboxCoreResults.Failure<bool>(MapError(exception));
        }
    }

    public async ValueTask<FreeboxCoreResult<bool>> DeleteAllLogEntriesAsync(
        CancellationToken cancellationToken = default)
    {
        LogOperationStarted(_logger, "DeleteAllLogEntries");
        try
        {
            await _client.Services.Call.DeleteAllLogEntriesAsync(cancellationToken).ConfigureAwait(false);
            LogMutationSucceeded(_logger, "DeleteAllLogEntries");
            return FreeboxCoreResults.Success(true);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            LogOperationFailed(_logger, "DeleteAllLogEntries", exception.Message);
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

    [LoggerMessage(EventId = 1450, Level = LogLevel.Debug, Message = "Calls service operation started: {Operation}.")]
    private static partial void LogOperationStarted(ILogger logger, string operation);

    [LoggerMessage(EventId = 1451, Level = LogLevel.Debug,
        Message = "Calls service operation succeeded: {Operation} (countHint={CountHint}).")]
    private static partial void LogOperationSucceeded(ILogger logger, string operation, int countHint);

    [LoggerMessage(EventId = 1452, Level = LogLevel.Information,
        Message = "Calls service mutation succeeded: {Operation}.")]
    private static partial void LogMutationSucceeded(ILogger logger, string operation);

    [LoggerMessage(EventId = 1453, Level = LogLevel.Warning,
        Message = "Calls service operation failed: {Operation}. Reason: {Reason}")]
    private static partial void LogOperationFailed(ILogger logger, string operation, string? reason);
}
