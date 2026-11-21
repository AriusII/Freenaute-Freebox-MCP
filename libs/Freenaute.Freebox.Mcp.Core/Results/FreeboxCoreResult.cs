namespace Freenaute.Freebox.Mcp.Core.Results;

public sealed record FreeboxCoreResult<T>
{
    private FreeboxCoreResult(T value)
    {
        Value = value;
        IsSuccess = true;
    }

    private FreeboxCoreResult(FreeboxCoreError error)
    {
        Error = error ?? throw new ArgumentNullException(nameof(error));
    }

    public T? Value { get; }
    public FreeboxCoreError? Error { get; }
    public bool IsSuccess { get; }

    internal static FreeboxCoreResult<T> Success(T value)
    {
        return new FreeboxCoreResult<T>(value);
    }

    internal static FreeboxCoreResult<T> Failure(FreeboxCoreError error)
    {
        return new FreeboxCoreResult<T>(error);
    }
}
