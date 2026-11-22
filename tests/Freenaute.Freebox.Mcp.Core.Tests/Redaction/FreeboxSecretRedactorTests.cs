namespace Freenaute.Freebox.Mcp.Core.Tests.Redaction;

public sealed class FreeboxSecretRedactorTests
{
    [Theory]
    [InlineData("app_token=alpha", "app_token=<redacted>")]
    [InlineData("appToken: 'alpha'", "appToken: '<redacted>'")]
    [InlineData("track_id = 42, keep=true", "track_id = <redacted>, keep=true")]
    [InlineData("PASSWORD:\"hunter2\"", "PASSWORD:\"<redacted>\"")]
    [InlineData("authorization: Bearer token", "authorization: <redacted>")]
    [InlineData("authorization:Bearer token, keep=true", "authorization:<redacted>, keep=true")]
    [InlineData("cookie='value'; next", "cookie='<redacted>'; next")]
    [InlineData("{\"app_token\":\"alpha\",\"status\":\"ok\"}", "{\"app_token\":\"<redacted>\",\"status\":\"ok\"}")]
    [InlineData("{\"sessionToken\":\"beta\",\"status\":\"ok\"}", "{\"sessionToken\":\"<redacted>\",\"status\":\"ok\"}")]
    [InlineData("{\"app_token\": \"alpha\"}", "{\"app_token\": \"<redacted>\"}")]
    [InlineData("APP_TOKEN=alpha", "APP_TOKEN=<redacted>")]
    [InlineData("Track_ID=7", "Track_ID=<redacted>")]
    [InlineData("password=x; app_token=y", "password=<redacted>; app_token=<redacted>")]
    [InlineData("sessionToken = beta", "sessionToken = <redacted>")]
    public void Redact_ReplacesSensitiveAssignmentValues(string input, string expected)
    {
        Assert.Equal(expected, FreeboxSecretRedactor.Redact(input));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Redact_EmptyOrNull_ReturnsEmpty(string? input)
    {
        Assert.Equal(string.Empty, FreeboxSecretRedactor.Redact(input));
    }

    [Fact]
    public void Redact_NonSensitiveText_IsUnchanged()
    {
        const string Input = "status=ok, device=Freebox";
        Assert.Equal(Input, FreeboxSecretRedactor.Redact(Input));
    }
}
