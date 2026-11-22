namespace Freenaute.Freebox.Mcp.Core.Services;

public sealed partial class FreeboxStartupAuthenticationService(
    IFreeboxOrchestrator orchestrator,
    IOptions<FreeboxCoreOptions> options,
    ILogger<FreeboxStartupAuthenticationService> logger) : IFreeboxStartupAuthenticationService
{
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
                LogCredentialAlreadyApproved(_logger);
                return;
            case FreeboxCredentialState.Unavailable:
                LogCredentialStorageUnavailable(_logger);
                return;
            case FreeboxCredentialState.Pending:
                await WaitForApprovalAsync(cancellationToken).ConfigureAwait(false);
                return;
            case FreeboxCredentialState.Missing:
            case FreeboxCredentialState.Denied:
            case FreeboxCredentialState.Expired:
            case FreeboxCredentialState.Corrupt:
            default:
                break;
        }

        if (!_options.AllowPairingInitiation)
        {
            LogPairingInitiationDisabled(_logger, status.Value.CredentialState);
            return;
        }

        FreeboxCoreResult<FreeboxPairingResult> begin = await _orchestrator
            .BeginPairingAsync(cancellationToken)
            .ConfigureAwait(false);
        if (!begin.IsSuccess)
        {
            LogUnableToInitiateStartupPairing(_logger, begin.Error?.Code);
            return;
        }

        await WaitForApprovalAsync(cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask WaitForApprovalAsync(CancellationToken cancellationToken)
    {
        FreeboxCoreResult<FreeboxCredentialResult> wait = await _orchestrator
            .WaitForApprovalAsync(_options.PairingPollInterval, _options.PairingWaitTimeout, cancellationToken)
            .ConfigureAwait(false);
        if (wait.IsSuccess)
        {
            LogStartupPairingWaitCompleted(_logger, wait.Value?.State);
        }
        else
        {
            LogStartupPairingWaitFailed(_logger, wait.Error?.Code);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Startup authentication is disabled by configuration.")]
    private static partial void LogStartupAuthenticationDisabled(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Unable to evaluate Freebox credential status at startup: {ErrorCode}")]
    private static partial void LogUnableToEvaluateCredentialStatus(ILogger logger, FreeboxCoreErrorCode? errorCode);

    [LoggerMessage(Level = LogLevel.Information, Message = "Freebox credential is already approved.")]
    private static partial void LogCredentialAlreadyApproved(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Freebox secure credential storage is unavailable; startup login is skipped.")]
    private static partial void LogCredentialStorageUnavailable(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Freebox credential state is {CredentialState}. Pairing initiation is disabled by policy.")]
    private static partial void LogPairingInitiationDisabled(ILogger logger, FreeboxCredentialState credentialState);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Unable to initiate startup pairing: {ErrorCode}")]
    private static partial void LogUnableToInitiateStartupPairing(ILogger logger, FreeboxCoreErrorCode? errorCode);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Startup pairing wait completed with state: {CredentialState}")]
    private static partial void LogStartupPairingWaitCompleted(ILogger logger, FreeboxCredentialState? credentialState);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Startup pairing wait failed with error: {ErrorCode}")]
    private static partial void LogStartupPairingWaitFailed(ILogger logger, FreeboxCoreErrorCode? errorCode);
}
