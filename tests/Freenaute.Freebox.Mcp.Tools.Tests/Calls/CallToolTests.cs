namespace Freenaute.Freebox.Mcp.Tools.Tests.Calls;

public sealed class CallToolTests
{
    [Fact]
    public async Task GetLogAsync_MapsEntriesAndExcludesPersonalFields()
    {
        var service = new RecordingCallsService(
        [
            new FreeboxCallEntrySnapshot(
                42, "missed\n", 1_700_000_000, 12, true, 7, 3)
        ]);
        var tools = new FreeboxCallTools(service);

        CallLogToolResult result = await tools.GetLogAsync(TestContext.Current.CancellationToken);

        CallLogEntryToolResult entry = Assert.Single(result.Entries);
        Assert.Equal("missed", entry.Type);
        Assert.Equal(1_700_000_000, entry.Timestamp);
        Assert.Equal(12, entry.DurationSeconds);
        Assert.True(entry.IsNew);
        Assert.NotEqual("42", entry.EntryId);
        Assert.NotEqual("7", entry.ContactId);
        Assert.NotEqual("3", entry.LineId);
    }

    [Fact]
    public async Task GetAccountAsync_HashesPhoneNumber()
    {
        var service = new RecordingCallsService(account: new FreeboxCallAccountSnapshot("+33123456789"));
        var tools = new FreeboxCallTools(service);

        CallAccountToolResult result = await tools.GetAccountAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(result.PhoneNumberId);
        Assert.DoesNotContain("331", result.PhoneNumberId, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Mutations_ReportSuccessAndFailure()
    {
        var success = new RecordingCallsService(markRead: true, clearLog: true);
        var tools = new FreeboxCallTools(success);

        CallMutationToolResult marked = await tools.MarkAllReadAsync(TestContext.Current.CancellationToken);
        CallMutationToolResult cleared = await tools.ClearLogAsync(TestContext.Current.CancellationToken);

        Assert.True(marked.Success);
        Assert.True(cleared.Success);
        Assert.Equal("All call-log entries marked as read.", marked.Status);
        Assert.Equal("Call log cleared.", cleared.Status);

        var failure = new RecordingCallsService(markReadError: true);
        var failingTools = new FreeboxCallTools(failure);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            failingTools.MarkAllReadAsync(TestContext.Current.CancellationToken));
        Assert.Contains("TransportFailure", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetLogAsync_MapsCoreFailureToToolError()
    {
        var service = new RecordingCallsService(logError: true);
        var tools = new FreeboxCallTools(service);

        InvalidOperationException exception =
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                tools.GetLogAsync(TestContext.Current.CancellationToken));

        Assert.Contains("InvalidRequest", exception.Message, StringComparison.Ordinal);
    }

    private sealed class RecordingCallsService(
        FreeboxCallEntrySnapshot[]? log = null,
        FreeboxCallAccountSnapshot? account = null,
        bool markRead = false,
        bool clearLog = false,
        bool logError = false,
        bool markReadError = false) : IFreeboxCallsService
    {
        public ValueTask<FreeboxCoreResult<FreeboxCallEntrySnapshot[]>> GetLogAsync(
            CancellationToken cancellationToken = default)
        {
            return logError
                ? ValueTask.FromResult(FreeboxCoreResults.Failure<FreeboxCallEntrySnapshot[]>(
                    new FreeboxCoreError(FreeboxCoreErrorCode.InvalidRequest, "invalid call log request")))
                : ValueTask.FromResult(FreeboxCoreResults.Success(log ?? []));
        }

        public ValueTask<FreeboxCoreResult<FreeboxCallAccountSnapshot?>> GetAccountAsync(
            CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(FreeboxCoreResults.Success(account));
        }

        public ValueTask<FreeboxCoreResult<bool>> MarkAllLogEntriesAsReadAsync(
            CancellationToken cancellationToken = default)
        {
            return markReadError
                ? ValueTask.FromResult(FreeboxCoreResults.Failure<bool>(
                    new FreeboxCoreError(FreeboxCoreErrorCode.TransportFailure, "call service unavailable")))
                : ValueTask.FromResult(FreeboxCoreResults.Success(markRead));
        }

        public ValueTask<FreeboxCoreResult<bool>> DeleteAllLogEntriesAsync(
            CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(FreeboxCoreResults.Success(clearLog));
        }
    }
}
