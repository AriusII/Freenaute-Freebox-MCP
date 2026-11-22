namespace Freenaute.Freebox.Mcp.Core.Services;

public sealed partial class FreeboxStartupAuthenticationHostedService(
    IFreeboxStartupAuthenticationService startupAuthenticationService,
    ILogger<FreeboxStartupAuthenticationHostedService> logger) : IHostedService
{
    private readonly ILogger<FreeboxStartupAuthenticationHostedService> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly IFreeboxStartupAuthenticationService _startupAuthenticationService =
        startupAuthenticationService ?? throw new ArgumentNullException(nameof(startupAuthenticationService));

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _startupAuthenticationService.EnsureAuthenticatedAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            LogStartupAuthenticationBootstrapFailed(_logger, exception);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Startup authentication bootstrap failed; the MCP server will continue running.")]
    private static partial void LogStartupAuthenticationBootstrapFailed(ILogger logger, Exception exception);
}
