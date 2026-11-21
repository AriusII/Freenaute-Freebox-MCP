namespace Freenaute.Freebox.Mcp.Security;

internal static class PairingPolicy
{
    public static bool IsEnabled(string? value)
    {
        return value is "1" or "true" or "TRUE";
    }
}
