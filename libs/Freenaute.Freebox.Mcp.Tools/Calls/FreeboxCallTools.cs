using Microsoft.Extensions.Logging.Abstractions;

namespace Freenaute.Freebox.Mcp.Tools.Calls;

[McpServerToolType]
public sealed partial class FreeboxCallTools(
    IFreeboxCallsService calls,
    ILogger<FreeboxCallTools>? logger = null)
{
    private const int MaxEntries = 128;

    private readonly ILogger<FreeboxCallTools> _logger =
        logger ?? NullLogger<FreeboxCallTools>.Instance;

    [McpServerTool(Name = "freebox_calls_log", Title = "Get Freebox call log", ReadOnly = true,
        Idempotent = true, Destructive = false, OpenWorld = true, UseStructuredContent = true,
        OutputSchemaType = typeof(CallLogToolResult))]
    [Description(
        "Returns a bounded sanitized call log. Numbers, names, and raw contact or line identifiers are excluded.")]
    public async Task<CallLogToolResult> GetLogAsync(CancellationToken cancellationToken)
    {
        LogToolStarted(_logger, "freebox_calls_log");
        FreeboxCoreResult<FreeboxCallEntrySnapshot[]> result =
            await calls.GetLogAsync(cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            LogToolFailed(_logger, "freebox_calls_log", result.Error?.Code);
            return ToolResultFactory.Failure<CallLogToolResult>(result.Error!);
        }

        CallLogEntryToolResult[] entries =
        [
            .. result.Value!
                .Take(MaxEntries)
                .Select(entry => new CallLogEntryToolResult(
                    entry.Id is null
                        ? "unknown"
                        : Safe.StableIdentifier(entry.Id.Value.ToString(CultureInfo.InvariantCulture)),
                    Safe.Text(entry.Type, 32),
                    entry.Datetime,
                    entry.Duration,
                    entry.IsNew,
                    entry.ContactId is null
                        ? null
                        : Safe.StableIdentifier(entry.ContactId.Value.ToString(CultureInfo.InvariantCulture)),
                    entry.LineId is null
                        ? null
                        : Safe.StableIdentifier(entry.LineId.Value.ToString(CultureInfo.InvariantCulture))))
        ];

        LogToolSucceeded(_logger, "freebox_calls_log", entries.Length);
        return new CallLogToolResult(entries);
    }

    [McpServerTool(Name = "freebox_calls_account", Title = "Get Freebox call account", ReadOnly = true,
        Idempotent = true, Destructive = false, OpenWorld = true, UseStructuredContent = true,
        OutputSchemaType = typeof(CallAccountToolResult))]
    [Description("Returns a hashed identifier for the configured telephony account without exposing its phone number.")]
    public async Task<CallAccountToolResult> GetAccountAsync(CancellationToken cancellationToken)
    {
        LogToolStarted(_logger, "freebox_calls_account");
        FreeboxCoreResult<FreeboxCallAccountSnapshot?> result =
            await calls.GetAccountAsync(cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            LogToolFailed(_logger, "freebox_calls_account", result.Error?.Code);
            return ToolResultFactory.Failure<CallAccountToolResult>(result.Error!);
        }

        FreeboxCallAccountSnapshot? account = result.Value;
        return new CallAccountToolResult(account?.PhoneNumber is null
            ? null
            : Safe.StableIdentifier(account.PhoneNumber));
    }

    [McpServerTool(Name = "freebox_calls_mark_all_read", Title = "Mark Freebox calls as read", ReadOnly = false,
        Idempotent = true, Destructive = false, OpenWorld = true, UseStructuredContent = true,
        OutputSchemaType = typeof(CallMutationToolResult))]
    [Description("Marks all Freebox call-log entries as read.")]
    public async Task<CallMutationToolResult> MarkAllReadAsync(CancellationToken cancellationToken)
    {
        LogToolStarted(_logger, "freebox_calls_mark_all_read");
        FreeboxCoreResult<bool> result =
            await calls.MarkAllLogEntriesAsReadAsync(cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            LogToolFailed(_logger, "freebox_calls_mark_all_read", result.Error?.Code);
            return ToolResultFactory.Failure<CallMutationToolResult>(result.Error!);
        }

        LogToolMutationSucceeded(_logger, "freebox_calls_mark_all_read", result.Value);
        return new CallMutationToolResult(result.Value,
            result.Value ? "All call-log entries marked as read." : "No call-log entries were changed.");
    }

    [McpServerTool(Name = "freebox_calls_clear_log", Title = "Clear Freebox call log", ReadOnly = false,
        Idempotent = true, Destructive = true, OpenWorld = true, UseStructuredContent = true,
        OutputSchemaType = typeof(CallMutationToolResult))]
    [Description("Deletes the complete Freebox call log.")]
    public async Task<CallMutationToolResult> ClearLogAsync(CancellationToken cancellationToken)
    {
        LogToolStarted(_logger, "freebox_calls_clear_log");
        FreeboxCoreResult<bool> result =
            await calls.DeleteAllLogEntriesAsync(cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            LogToolFailed(_logger, "freebox_calls_clear_log", result.Error?.Code);
            return ToolResultFactory.Failure<CallMutationToolResult>(result.Error!);
        }

        LogToolMutationSucceeded(_logger, "freebox_calls_clear_log", result.Value);
        return new CallMutationToolResult(result.Value,
            result.Value ? "Call log cleared." : "Call log was not changed.");
    }

    [LoggerMessage(EventId = 1400, Level = LogLevel.Information, Message = "Executing {ToolName}.")]
    private static partial void LogToolStarted(ILogger logger, string toolName);

    [LoggerMessage(EventId = 1401, Level = LogLevel.Warning, Message = "{ToolName} failed: {ErrorCode}")]
    private static partial void LogToolFailed(ILogger logger, string toolName, FreeboxCoreErrorCode? errorCode);

    [LoggerMessage(EventId = 1402, Level = LogLevel.Information, Message = "{ToolName} succeeded (count={Count}).")]
    private static partial void LogToolSucceeded(ILogger logger, string toolName, int count);

    [LoggerMessage(EventId = 1403, Level = LogLevel.Information,
        Message = "{ToolName} mutation succeeded (changed={Changed}).")]
    private static partial void LogToolMutationSucceeded(ILogger logger, string toolName, bool changed);
}
