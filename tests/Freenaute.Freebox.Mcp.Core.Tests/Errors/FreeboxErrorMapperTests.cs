namespace Freenaute.Freebox.Mcp.Core.Tests.Errors;

public sealed class FreeboxErrorMapperTests
{
    public static TheoryData<Exception> InvalidRequestExceptions =>
    [
        new ArgumentException("password=secret"),
        new InvalidOperationException("app_token=secret")
    ];

    [Fact]
    public void Map_Null_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => FreeboxErrorMapper.Map(null!));
    }

    [Fact]
    public void Map_Cancellation_PreservesCorrelationId()
    {
        FreeboxCoreError error = FreeboxErrorMapper.Map(new OperationCanceledException(), "request-1");
        Assert.Equal(FreeboxCoreErrorCode.Cancelled, error.Code);
        Assert.Equal("request-1", error.CorrelationId);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, FreeboxCoreErrorCode.AuthenticationRequired)]
    [InlineData(HttpStatusCode.Forbidden, FreeboxCoreErrorCode.AuthenticationRequired)]
    [InlineData(HttpStatusCode.BadGateway, FreeboxCoreErrorCode.ApiRejected)]
    [InlineData(null, FreeboxCoreErrorCode.ApiRejected)]
    public void Map_FreeboxApiException_MapsStatusAndRedactsMessage(HttpStatusCode? status,
        FreeboxCoreErrorCode expected)
    {
        ConstructorInfo constructor = Assert.Single(typeof(FreeboxApiException).GetConstructors(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
        object?[] arguments =
        [
            .. Enumerable.Range(0, constructor.GetParameters().Length)
                .Select<int, object?>(index => index switch
                {
                    0 => status,
                    1 => "api_rejected",
                    2 => "session_token=secret",
                    _ => null
                })
        ];
        FreeboxApiException exception = Assert.IsType<FreeboxApiException>(constructor.Invoke(arguments));
        FreeboxCoreError error = FreeboxErrorMapper.Map(exception, "corr");
        Assert.Equal(expected, error.Code);
        Assert.Equal("session_token=<redacted>", error.Message);
        Assert.Equal(status is null ? 0 : (int)status.Value, error.HttpStatusCode);
        Assert.Equal("api_rejected", error.ApiErrorCode);
        Assert.Equal("corr", error.CorrelationId);
    }

    [Theory]
    [InlineData("Pending", FreeboxCoreErrorCode.AuthorizationPending)]
    [InlineData("Denied", FreeboxCoreErrorCode.AuthorizationDenied)]
    [InlineData("Unknown", FreeboxCoreErrorCode.AuthorizationPending)]
    public void Map_FreeboxAuthorizationException_MapsAuthorizationState(string status, FreeboxCoreErrorCode expected)
    {
        FreeboxAuthorizationException exception =
            new(Enum.Parse<AuthorizationTrackEnum>(status));
        Assert.Equal(expected, FreeboxErrorMapper.Map(exception).Code);
    }

    [Fact]
    public void Map_AuthenticationException_UsesAuthenticationRequired()
    {
        Assert.Equal(FreeboxCoreErrorCode.AuthenticationRequired,
            FreeboxErrorMapper.Map(new FreeboxAuthenticationException("authentication required")).Code);
    }

    [Fact]
    public void Map_HttpRequestException_UsesTransportFailureAndStatus()
    {
        FreeboxCoreError error = FreeboxErrorMapper.Map(
            new HttpRequestException("offline", null, HttpStatusCode.BadGateway));
        Assert.Equal(FreeboxCoreErrorCode.TransportFailure, error.Code);
        Assert.Equal(502, error.HttpStatusCode);
    }

    [Theory]
    [MemberData(nameof(InvalidRequestExceptions))]
    public void Map_RequestExceptions_RedactMessage(Exception exception)
    {
        FreeboxCoreError error = FreeboxErrorMapper.Map(exception);
        Assert.Equal(FreeboxCoreErrorCode.InvalidRequest, error.Code);
        Assert.DoesNotContain("secret", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<redacted>", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Map_UnknownException_DoesNotLeakMessage()
    {
        FreeboxCoreError error = FreeboxErrorMapper.Map(new Exception("secret"));
        Assert.Equal(FreeboxCoreErrorCode.Unknown, error.Code);
        Assert.Equal("The Freebox operation failed unexpectedly.", error.Message);
    }
}
