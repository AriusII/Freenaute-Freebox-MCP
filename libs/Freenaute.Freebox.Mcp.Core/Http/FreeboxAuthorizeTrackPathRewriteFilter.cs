namespace Freenaute.Freebox.Mcp.Core.Http;

public sealed class FreeboxAuthorizeTrackPathRewriteFilter : IHttpMessageHandlerBuilderFilter
{
    public Action<HttpMessageHandlerBuilder> Configure(Action<HttpMessageHandlerBuilder> next)
    {
        ArgumentNullException.ThrowIfNull(next);

        return builder =>
        {
            next(builder);
            builder.AdditionalHandlers.Add(new FreeboxAuthorizeTrackPathRewriteHandler());
        };
    }

    internal static bool TryRewriteAuthorizeTrackUri(Uri? uri, out Uri? rewrittenUri)
    {
        rewrittenUri = null;
        if (uri is null)
        {
            return false;
        }

        string path = uri.AbsolutePath;
        if (!path.EndsWith('/', StringComparison.Ordinal) ||
            !Regex.IsMatch(
                path,
                @"^/api/v\d+/login/authorize/\d+/$",
                RegexOptions.CultureInvariant))
        {
            return false;
        }

        var uriBuilder = new UriBuilder(uri)
        {
            Path = path[..^1]
        };
        rewrittenUri = uriBuilder.Uri;
        return true;
    }

    private sealed class FreeboxAuthorizeTrackPathRewriteHandler : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request.Method == HttpMethod.Get &&
                TryRewriteAuthorizeTrackUri(request.RequestUri, out Uri? rewrittenUri))
            {
                request.RequestUri = rewrittenUri;
            }

            return base.SendAsync(request, cancellationToken);
        }
    }
}
