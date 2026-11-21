namespace Freenaute.Freebox.Mcp.Core.Security;

public static partial class FreeboxSecretRedactor
{
    [GeneratedRegex(
        """(?i)("(?:app_token|appToken|track_id|trackId|session_token|sessionToken|challenge|password|authorization|cookie)"\s*:\s*")([^"]*)(")""",
        RegexOptions.CultureInvariant)]
    private static partial Regex JsonStringValuePattern();

    [GeneratedRegex(
        """(?i)('(?:app_token|appToken|track_id|trackId|session_token|sessionToken|challenge|password|authorization|cookie)'\s*[:=]\s*')([^']*)(')""",
        RegexOptions.CultureInvariant)]
    private static partial Regex SingleQuotedValuePattern();

    [GeneratedRegex(
        """(?i)((?:app_token|appToken|track_id|trackId|session_token|sessionToken|challenge|password|authorization|cookie)\s*[:=]\s*["']?)[^\s,;"'}]+""",
        RegexOptions.CultureInvariant)]
    private static partial Regex AssignmentValuePattern();

    public static string Redact(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        string redacted = JsonStringValuePattern().Replace(
            value,
            match => $"{match.Groups[1].Value}<redacted>{match.Groups[3].Value}");

        redacted = SingleQuotedValuePattern().Replace(
            redacted,
            match => $"{match.Groups[1].Value}<redacted>{match.Groups[3].Value}");

        redacted = AssignmentValuePattern().Replace(
            redacted,
            match => $"{match.Groups[1].Value}<redacted>");

        return redacted;
    }
}
