namespace Freenaute.Freebox.Mcp.Core.Services;

public sealed partial class FreeboxStartupAuthenticationService(
    IFreeboxOrchestrator orchestrator,
    IOptions<FreeboxCoreOptions> options,
    ILogger<FreeboxStartupAuthenticationService> logger) : IFreeboxStartupAuthenticationService
{
    private static readonly TimeSpan StartupMaxWaitTimeout = TimeSpan.FromMinutes(2);

    private readonly ILogger<FreeboxStartupAuthenticationService> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly FreeboxCoreOptions _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

    private readonly IFreeboxOrchestrator _orchestrator =
        orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));

    public async ValueTask EnsureAuthenticatedAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.AuthenticateOnStartup)
        {
            LogStartupAuthenticationDisabled(_logger);
            return;
        }

        FreeboxCoreResult<FreeboxConnectionResult> status = await _orchestrator
            .GetConnectionStatusAsync(cancellationToken)
            .ConfigureAwait(false);

        if (!status.IsSuccess)
        {
            LogUnableToEvaluateCredentialStatus(_logger, status.Error?.Code);
            return;
        }

        switch (status.Value!.CredentialState)
        {
            case FreeboxCredentialState.Approved:
                await EnsureApprovedCredentialCanAuthenticateAsync(cancellationToken).ConfigureAwait(false);
                return;
            case FreeboxCredentialState.Unavailable:
                LogCredentialStorageUnavailable(_logger);
                return;
            case FreeboxCredentialState.Pending:
                LogPendingCredentialFound(_logger);
                FreeboxCoreResult<FreeboxCredentialResult> pendingWait = await WaitForApprovalAsync(cancellationToken)
                    .ConfigureAwait(false);
                if (!pendingWait.IsSuccess && pendingWait.Error?.Code == FreeboxCoreErrorCode.ApiRejected)
                {
                    LogStalePairingTrackDetected(_logger);
                    if (!await TryResetStalePairingStateAsync(cancellationToken).ConfigureAwait(false))
                    {
                        return;
                    }

                    await InitiateAndWaitForStartupPairingAsync(cancellationToken).ConfigureAwait(false);
                }

                return;
            case FreeboxCredentialState.Missing:
                LogCredentialMissingAwaitingPhysicalApproval(_logger);
                break;
            case FreeboxCredentialState.Denied:
                LogCredentialDeniedAwaitingPhysicalApproval(_logger);
                break;
            case FreeboxCredentialState.Expired:
                LogCredentialExpiredAwaitingPhysicalApproval(_logger);
                break;
            case FreeboxCredentialState.Corrupt:
                LogCredentialCorruptAwaitingPhysicalApproval(_logger);
                break;
        }

        await InitiateAndWaitForStartupPairingAsync(cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask<FreeboxCoreResult<FreeboxCredentialResult>> WaitForApprovalAsync(
        CancellationToken cancellationToken)
    {
        TimeSpan effectiveWaitTimeout = _options.PairingWaitTimeout <= StartupMaxWaitTimeout
            ? _options.PairingWaitTimeout
            : StartupMaxWaitTimeout;
        if (effectiveWaitTimeout != _options.PairingWaitTimeout)
        {
            LogStartupWaitTimeoutClamped(_logger, _options.PairingWaitTimeout.TotalSeconds,
                StartupMaxWaitTimeout.TotalSeconds);
        }

        FreeboxCoreResult<FreeboxCredentialResult> wait = await _orchestrator
            .WaitForApprovalAsync(_options.PairingPollInterval, effectiveWaitTimeout, cancellationToken)
            .ConfigureAwait(false);
        if (wait.IsSuccess)
        {
            LogStartupPairingWaitCompleted(_logger, wait.Value?.State);
        }
        else
        {
            LogStartupPairingWaitFailed(_logger, wait.Error?.Code);
        }

        return wait;
    }

    private async ValueTask EnsureApprovedCredentialCanAuthenticateAsync(CancellationToken cancellationToken)
    {
        FreeboxCoreResult<FreeboxSystemSnapshot> authenticatedProbe = await _orchestrator
            .GetSystemSnapshotAsync(cancellationToken)
            .ConfigureAwait(false);
        if (authenticatedProbe.IsSuccess)
        {
            LogAutomaticConnectionSucceeded(_logger);
            return;
        }

        if (authenticatedProbe.Error?.Code == FreeboxCoreErrorCode.AuthenticationRequired)
        {
            LogStoredCredentialRequiresRenewal(_logger);
            await InitiateAndWaitForStartupPairingAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        LogApprovedCredentialProbeFailed(_logger, authenticatedProbe.Error?.Code);
    }

    private async ValueTask InitiateAndWaitForStartupPairingAsync(CancellationToken cancellationToken)
    {
        FreeboxCoreResult<FreeboxPairingResult> begin = await _orchestrator
            .BeginStartupPairingAsync(cancellationToken)
            .ConfigureAwait(false);
        if (!begin.IsSuccess)
        {
            LogUnableToInitiateStartupPairing(_logger, begin.Error?.Code);
            return;
        }

        FreeboxCoreResult<FreeboxCredentialResult> wait = await WaitForApprovalAsync(cancellationToken)
            .ConfigureAwait(false);
        if (wait.IsSuccess || wait.Error?.Code != FreeboxCoreErrorCode.ApiRejected)
        {
            return;
        }

        if (!await TryResetStalePairingStateAsync(cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        LogRetryingStartupPairingAfterApiRejection(_logger);
        FreeboxCoreResult<FreeboxPairingResult> retryBegin = await _orchestrator
            .BeginStartupPairingAsync(cancellationToken)
            .ConfigureAwait(false);
        if (!retryBegin.IsSuccess)
        {
            LogUnableToInitiateStartupPairing(_logger, retryBegin.Error?.Code);
            return;
        }

        await WaitForApprovalAsync(cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask<bool> TryResetStalePairingStateAsync(CancellationToken cancellationToken)
    {
        FreeboxCoreResult<bool> forget = await _orchestrator.ForgetCredentialsAsync(cancellationToken)
            .ConfigureAwait(false);
        if (!forget.IsSuccess)
        {
            LogUnableToResetStalePairingState(_logger, forget.Error?.Code);
            return false;
        }

        LogStalePairingStateReset(_logger);
        return true;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Startup authentication is disabled by configuration.")]
    private static partial void LogStartupAuthenticationDisabled(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Unable to evaluate Freebox credential status at startup: {ErrorCode}")]
    private static partial void LogUnableToEvaluateCredentialStatus(ILogger logger, FreeboxCoreErrorCode? errorCode);

    [LoggerMessage(Level = LogLevel.Information, Message = "Freebox credential is already approved.")]
    private static partial void LogAutomaticConnectionSucceeded(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Freebox secure credential storage is unavailable; startup login is skipped.")]
    private static partial void LogCredentialStorageUnavailable(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Unable to initiate startup pairing: {ErrorCode}")]
    private static partial void LogUnableToInitiateStartupPairing(ILogger logger, FreeboxCoreErrorCode? errorCode);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Startup pairing wait completed with state: {CredentialState}")]
    private static partial void LogStartupPairingWaitCompleted(ILogger logger, FreeboxCredentialState? credentialState);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Startup pairing wait failed with error: {ErrorCode}")]
    private static partial void LogStartupPairingWaitFailed(ILogger logger, FreeboxCoreErrorCode? errorCode);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Startup pairing wait timeout clamped from {ConfiguredSeconds}s to {AppliedSeconds}s.")]
    private static partial void LogStartupWaitTimeoutClamped(
        ILogger logger,
        double configuredSeconds,
        double appliedSeconds);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Retrying startup pairing after API rejection from pairing wait.")]
    private static partial void LogRetryingStartupPairingAfterApiRejection(ILogger logger);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Stored Freebox credential requires renewal; startup pairing will request physical approval.")]
    private static partial void LogStoredCredentialRequiresRenewal(ILogger logger);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Unable to validate stored Freebox credential at startup: {ErrorCode}")]
    private static partial void LogApprovedCredentialProbeFailed(ILogger logger, FreeboxCoreErrorCode? errorCode);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Pending Freebox pairing token detected. Waiting for physical approval.")]
    private static partial void LogPendingCredentialFound(ILogger logger);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message =
            "Pending Freebox pairing track appears stale (API rejected previous track id). Starting a fresh pairing request.")]
    private static partial void LogStalePairingTrackDetected(ILogger logger);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Unable to reset stale pairing state before retry: {ErrorCode}")]
    private static partial void LogUnableToResetStalePairingState(ILogger logger, FreeboxCoreErrorCode? errorCode);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Stale pairing state cleared from secure storage.")]
    private static partial void LogStalePairingStateReset(ILogger logger);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "No persisted Freebox token found. Startup pairing requires physical approval on the Freebox.")]
    private static partial void LogCredentialMissingAwaitingPhysicalApproval(ILogger logger);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Stored Freebox token was denied. Startup pairing requires physical approval on the Freebox.")]
    private static partial void LogCredentialDeniedAwaitingPhysicalApproval(ILogger logger);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Stored Freebox token expired. Startup pairing requires physical approval on the Freebox.")]
    private static partial void LogCredentialExpiredAwaitingPhysicalApproval(ILogger logger);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Stored Freebox token is corrupt. Startup pairing requires physical approval on the Freebox.")]
    private static partial void LogCredentialCorruptAwaitingPhysicalApproval(ILogger logger);
}
