using Freenaute.Freebox.Mcp.Core.Http;
namespace Freenaute.Freebox.Mcp.Core.Tests.Http;

public sealed class FreeboxAuthorizeTrackPathRewriteFilterTests
{
    [Fact]
    public void TryRewriteAuthorizeTrackUri_RewritesTrailingSlashTrackPath()
    {
        Uri source = new("https://mafreebox.freebox.fr/api/v16/login/authorize/42/");

        bool rewritten = FreeboxAuthorizeTrackPathRewriteFilter.TryRewriteAuthorizeTrackUri(source, out Uri? result);

        Assert.True(rewritten);
        Assert.NotNull(result);
        Assert.Equal("https://mafreebox.freebox.fr/api/v16/login/authorize/42", result.AbsoluteUri);
    }

    [Fact]
    public void TryRewriteAuthorizeTrackUri_DoesNotRewriteNonTrackPath()
    {
        Uri source = new("https://mafreebox.freebox.fr/api/v16/login/authorize/");

        bool rewritten = FreeboxAuthorizeTrackPathRewriteFilter.TryRewriteAuthorizeTrackUri(source, out Uri? result);

        Assert.False(rewritten);
        Assert.Null(result);
    }
}
