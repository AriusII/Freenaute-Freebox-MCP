namespace Freenaute.Freebox.Mcp.Tools.Security;

internal static class Safe
{
    public static string Text(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        string normalized = new(value.Where(character => !char.IsControl(character)).ToArray());
        return normalized.Length <= maxLength ? normalized : normalized[..maxLength];
    }

    public static string StableIdentifier(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "unknown";
        }

        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexStringLower(hash);
    }
}
