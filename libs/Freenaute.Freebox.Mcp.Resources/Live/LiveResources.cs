namespace Freenaute.Freebox.Mcp.Resources.Live;

[McpServerResourceType]
public sealed class LiveResources(IFreeboxLiveResourceSource? source = null)
{
    private const int MaxCharacters = 64 * 1024;
    private static readonly TimeSpan SourceTimeout = TimeSpan.FromSeconds(10);

    [McpServerResource(
        UriTemplate = "freebox://live/status",
        Name = "freebox-live-status",
        Title = "Current sanitized Freebox status",
        MimeType = "text/markdown")]
    public async ValueTask<TextResourceContents> GetStatusAsync(CancellationToken cancellationToken)
    {
        (string content, string? mediaType) = await ReadSourceAsync(cancellationToken).ConfigureAwait(false);
        string sanitized = Sanitize(content);
        bool truncated = sanitized.Length > MaxCharacters;
        string bounded = truncated ? sanitized[..MaxCharacters] : sanitized;
        if (truncated)
        {
            bounded += "\n\n[resource content truncated after 64 KiB]\n";
        }

        return new TextResourceContents
        {
            Uri = "freebox://live/status",
            MimeType = mediaType is null ? "text/markdown" : NormalizeMediaType(mediaType),
            Text = bounded
        };
    }

    private async ValueTask<(string Content, string? MediaType)> ReadSourceAsync(CancellationToken cancellationToken)
    {
        if (source is null)
        {
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
            return (
                "# Freebox status\n\nLive data is unavailable: the Freebox did not respond within 10 seconds.\n",
                null);
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            // Never leak exception details here: they may contain URLs, tokens or host names.
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
}
