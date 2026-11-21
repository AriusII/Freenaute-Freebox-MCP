namespace Freenaute.Freebox.Mcp.Core.Results;

public static class FreeboxCoreResults
{
    public static FreeboxCoreResult<T> Success<T>(T value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return FreeboxCoreResult<T>.Success(value);
    }

    public static FreeboxCoreResult<T> Failure<T>(FreeboxCoreError error)
    {
        return FreeboxCoreResult<T>.Failure(error);
    }
}
