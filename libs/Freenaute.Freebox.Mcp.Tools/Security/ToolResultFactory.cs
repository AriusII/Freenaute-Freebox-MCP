namespace Freenaute.Freebox.Mcp.Tools.Security;

internal static class ToolResultFactory
{
    public static T Failure<T>(FreeboxCoreError error) where T : class
    {
        throw new InvalidOperationException($"{error.Code}: {error.Message}");
    }
}
