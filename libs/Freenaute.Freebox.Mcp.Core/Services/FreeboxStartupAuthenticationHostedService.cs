namespace Freenaute.Freebox.Mcp.Core.Services;

public sealed partial class FreeboxStartupAuthenticationHostedService(
    IFreeboxStartupAuthenticationService startupAuthenticationService,
    ILogger<FreeboxStartupAuthenticationHostedService> logger) : IHostedService
{
    private readonly ILogger<FreeboxStartupAuthenticationHostedService> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly IFreeboxStartupAuthenticationService _startupAuthenticationService =
        startupAuthenticationService ?? throw new ArgumentNullException(nameof(startupAuthenticationService));

    private CancellationTokenSource? _startupCts;
    private Task? _startupTask;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _startupCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _startupTask = RunStartupBootstrapAsync(_startupCts.Token);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _startupCts?.Cancel();
        if (_startupTask is not null)
        {
            await _startupTask.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task RunStartupBootstrapAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _startupAuthenticationService.EnsureAuthenticatedAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Graceful shutdown or startup cancellation.
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            LogStartupAuthenticationBootstrapFailed(_logger, exception);
        }
    }

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Startup authentication bootstrap failed; the MCP server will continue running.")]
    private static partial void LogStartupAuthenticationBootstrapFailed(ILogger logger, Exception exception);
}
