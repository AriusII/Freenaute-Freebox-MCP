namespace Freenaute.Freebox.Mcp.Core.Tests.Results;

public sealed class FreeboxCoreResultTests
{
    [Fact]
    public void Success_SetsValueAndSuccessState()
    {
        FreeboxCoreResult<string> result = FreeboxCoreResults.Success("ok");
        Assert.True(result.IsSuccess);
        Assert.Equal("ok", result.Value);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Failure_SetsErrorAndFailureState()
    {
        var expected = new FreeboxCoreError(FreeboxCoreErrorCode.InvalidRequest, "bad");
        FreeboxCoreResult<string> result = FreeboxCoreResults.Failure<string>(expected);
        Assert.False(result.IsSuccess);
        Assert.Null(result.Value);
        Assert.Same(expected, result.Error);
    }

    [Fact]
    public void CoreJsonContext_SerializesCamelCaseValuesAndStringEnums()
    {
        var value = new FreeboxCoreError(FreeboxCoreErrorCode.ApiRejected, "no", 503, "api_error", "corr");
        string json = JsonSerializer.Serialize(value, FreeboxCoreJsonContext.Default.FreeboxCoreError);
        Assert.Contains("\"code\":\"ApiRejected\"", json, StringComparison.Ordinal);
        Assert.Contains("\"httpStatusCode\":503", json, StringComparison.Ordinal);
        Assert.Contains("\"apiErrorCode\":\"api_error\"", json, StringComparison.Ordinal);
    }
}
