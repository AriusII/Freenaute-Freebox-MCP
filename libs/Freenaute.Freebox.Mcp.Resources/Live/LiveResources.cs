using Microsoft.Extensions.Logging.Abstractions;

namespace Freenaute.Freebox.Mcp.Resources.Live;

[McpServerResourceType]
public sealed partial class LiveResources(
    IFreeboxLiveResourceSource? source = null,
    ILogger<LiveResources>? logger = null)
{
    private const int MaxCharacters = 64 * 1024;
    private static readonly TimeSpan SourceTimeout = TimeSpan.FromSeconds(10);
    private readonly ILogger<LiveResources> _logger = logger ?? NullLogger<LiveResources>.Instance;

    [McpServerResource(
        UriTemplate = "freebox://live/status",
        Name = "freebox-live-status",
        Title = "Current sanitized Freebox status",
        MimeType = "text/markdown")]
    public async ValueTask<TextResourceContents> GetStatusAsync(CancellationToken cancellationToken)
    {
        LogLiveResourceReadStarted(_logger);
        (string content, string? mediaType) = await ReadSourceAsync(cancellationToken).ConfigureAwait(false);
        return FormatLiveResource("freebox://live/status", content, mediaType);
    }

    [McpServerResource(UriTemplate = "freebox://live/network-advanced", Name = "freebox-live-network-advanced", Title = "Advanced live network summary", MimeType = "text/markdown")]
    public async ValueTask<TextResourceContents> GetNetworkAdvancedAsync(CancellationToken cancellationToken) =>
        await GetDomainLiveResourceAsync("freebox://live/network-advanced", "# Freebox advanced network summary", cancellationToken).ConfigureAwait(false);

    [McpServerResource(UriTemplate = "freebox://live/files-advanced", Name = "freebox-live-files-advanced", Title = "Advanced live files summary", MimeType = "text/markdown")]
    public async ValueTask<TextResourceContents> GetFilesAdvancedAsync(CancellationToken cancellationToken) =>
        await GetDomainLiveResourceAsync("freebox://live/files-advanced", "# Freebox advanced files summary", cancellationToken).ConfigureAwait(false);

    [McpServerResource(UriTemplate = "freebox://live/systemhome-controls", Name = "freebox-live-systemhome-controls", Title = "Live System and home controls summary", MimeType = "text/markdown")]
    public async ValueTask<TextResourceContents> GetSystemHomeControlsAsync(CancellationToken cancellationToken) =>
        await GetDomainLiveResourceAsync("freebox://live/systemhome-controls", "# Freebox System and home controls summary", cancellationToken).ConfigureAwait(false);

    [McpServerResource(UriTemplate = "freebox://live/services-controls", Name = "freebox-live-services-controls", Title = "Live services controls summary", MimeType = "text/markdown")]
    public async ValueTask<TextResourceContents> GetServicesControlsAsync(CancellationToken cancellationToken) =>
        await GetDomainLiveResourceAsync("freebox://live/services-controls", "# Freebox services controls summary", cancellationToken).ConfigureAwait(false);

    [McpServerResource(UriTemplate = "freebox://live/parental-write", Name = "freebox-live-parental-write", Title = "Live parental controls write summary", MimeType = "text/markdown")]
    public async ValueTask<TextResourceContents> GetParentalWriteAsync(CancellationToken cancellationToken) =>
        await GetDomainLiveResourceAsync("freebox://live/parental-write", "# Freebox parental controls write summary", cancellationToken).ConfigureAwait(false);

    private async ValueTask<TextResourceContents> GetDomainLiveResourceAsync(
        string uri,
        string fallbackTitle,
        CancellationToken cancellationToken)
    {
        LogLiveResourceReadStarted(_logger);
        (string content, string? mediaType) = await ReadSourceAsync(cancellationToken).ConfigureAwait(false);
        if (content.Contains("Live data is unavailable", StringComparison.Ordinal))
        {
            content = $"{fallbackTitle}\n\n{content[(content.IndexOf('\n') + 1)..].TrimStart()}";
        }

        return FormatLiveResource(uri, content, mediaType);
    }

    private TextResourceContents FormatLiveResource(string uri, string content, string? mediaType)
    {
        string sanitized = Sanitize(content);
        bool truncated = sanitized.Length > MaxCharacters;
        string bounded = truncated ? sanitized[..MaxCharacters] : sanitized;
        if (truncated)
        {
            bounded += "\n\n[resource content truncated after 64 KiB]\n";
        }

        LogLiveResourceReadSucceeded(_logger, bounded.Length, truncated);
        return new TextResourceContents
        {
            Uri = uri,
            MimeType = mediaType is null ? "text/markdown" : NormalizeMediaType(mediaType),
            Text = bounded
        };
    }

    private async ValueTask<(string Content, string? MediaType)> ReadSourceAsync(CancellationToken cancellationToken)
    {
        if (source is null)
        {
            LogLiveSourceUnavailable(_logger);
            return (
                "# Freebox status\n\nLive data is unavailable because no `IFreeboxLiveResourceSource` is registered.\n",
                null);
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(SourceTimeout);

        try
        {
            FreeboxLiveSnapshot snapshot = await source.GetSnapshotAsync(timeout.Token).ConfigureAwait(false);
            return (snapshot.Content, snapshot.MediaType);
        }
        catch (OperationCanceledException) when (timeout.IsCancellationRequested)
        {
            LogLiveSourceTimeout(_logger, SourceTimeout.TotalSeconds);
            return (
                "# Freebox status\n\nLive data is unavailable: the Freebox did not respond within 10 seconds.\n",
                null);
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            // Never leak exception details here: they may contain URLs, tokens or host names.
            LogLiveSourceFailed(_logger, exception.Message);
            return ("# Freebox status\n\nLive data is currently unavailable.\n", null);
        }
    }

    private static string Sanitize(string content)
    {
        // Defense in depth on top of the source allow-list: redact secret-looking assignments
        // and strip control characters (keep \n, \r, \t) before bounding the payload.
        string redacted = FreeboxSecretRedactor.Redact(content);
        var builder = new StringBuilder(redacted.Length);
        foreach (char character in redacted)
        {
            builder.Append(character switch
            {
                '\n' or '\r' or '\t' => character,
                _ when char.IsControl(character) => ' ',
                _ => character
            });
        }

        return builder.ToString();
    }

    private static string NormalizeMediaType(string mediaType)
    {
        string normalized = mediaType.Trim();
        if (normalized.Length is 0 or > 128 || normalized.Any(static character => character is '\r' or '\n' or '\0'))
        {
            return "text/plain";
        }

        return normalized;
    }

    [LoggerMessage(EventId = 3000, Level = LogLevel.Information,
        Message = "Reading live resource freebox://live/status.")]
    private static partial void LogLiveResourceReadStarted(ILogger logger);

    [LoggerMessage(EventId = 3001, Level = LogLevel.Information,
        Message = "Live resource read succeeded (length={Length}, truncated={Truncated}).")]
    private static partial void LogLiveResourceReadSucceeded(ILogger logger, int length, bool truncated);

    [LoggerMessage(EventId = 3002, Level = LogLevel.Warning,
        Message = "Live resource source unavailable: no IFreeboxLiveResourceSource registered.")]
    private static partial void LogLiveSourceUnavailable(ILogger logger);

    [LoggerMessage(EventId = 3003, Level = LogLevel.Warning,
        Message = "Live resource source timeout after {TimeoutSeconds} seconds.")]
    private static partial void LogLiveSourceTimeout(ILogger logger, double timeoutSeconds);

    [LoggerMessage(EventId = 3004, Level = LogLevel.Warning, Message = "Live resource source failed: {Reason}")]
    private static partial void LogLiveSourceFailed(ILogger logger, string? reason);
}
