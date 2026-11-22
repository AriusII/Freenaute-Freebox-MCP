namespace Freenaute.Freebox.Mcp.Hosting;

internal static class McpStartupEnvironment
{
    public static bool TryRead(out McpStartupOptions options)
    {
        options = default!;
        if (!TryReadPositiveInt("FREEBOX_PAIRING_POLL_INTERVAL_SECONDS", out int? pairingPollIntervalSeconds))
        {
            return false;
        }

        if (!TryReadPositiveInt("FREEBOX_PAIRING_WAIT_TIMEOUT_SECONDS", out int? pairingWaitTimeoutSeconds))
        {
            return false;
        }

        if (!TryReadBoolean("FREEBOX_AUTHENTICATE_ON_STARTUP", out bool? authenticateOnStartup))
        {
            return false;
        }

        if (!TryReadServerAddress(out Uri? serverAddress))
        {
            return false;
        }

        options = new McpStartupOptions(
            Environment.GetEnvironmentVariable("FREEBOX_APPLICATION_ID") ?? BuildInfo.ApplicationId,
            Environment.GetEnvironmentVariable("FREEBOX_APPLICATION_NAME") ?? "Freenaute Freebox MCP",
            Environment.GetEnvironmentVariable("FREEBOX_DEVICE_NAME") ?? Environment.MachineName,
            Environment.GetEnvironmentVariable("FREEBOX_CREDENTIAL_PROFILE") ?? "default",
            serverAddress,
            pairingPollIntervalSeconds,
            pairingWaitTimeoutSeconds,
            authenticateOnStartup,
            PairingPolicy.IsEnabled(Environment.GetEnvironmentVariable("FREEBOX_ALLOW_PAIRING_INITIATION")));
        return true;
    }

    private static bool TryReadPositiveInt(string variableName, out int? value)
    {
        value = null;
        string? rawValue = Environment.GetEnvironmentVariable(variableName);
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return true;
        }

        if (int.TryParse(rawValue, NumberStyles.None, CultureInfo.InvariantCulture, out int parsedValue) &&
            parsedValue > 0)
        {
            value = parsedValue;
            return true;
        }

        Console.Error.WriteLine($"{variableName} must be a positive integer number of seconds.");
        return false;
    }

    private static bool TryReadBoolean(string variableName, out bool? value)
    {
        value = null;
        string? rawValue = Environment.GetEnvironmentVariable(variableName);
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return true;
        }

        value = rawValue switch
        {
            "1" => true,
            "0" => false,
            _ when bool.TryParse(rawValue, out bool parsed) => parsed,
            _ => null
        };
        if (value is not null)
        {
            return true;
        }

        Console.Error.WriteLine($"{variableName} must be a boolean (true/false/1/0).");
        return false;
    }

    private static bool TryReadServerAddress(out Uri? serverAddress)
    {
        serverAddress = null;
        string? rawValue = Environment.GetEnvironmentVariable("FREEBOX_SERVER_ADDRESS");
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return true;
        }

        if (Uri.TryCreate(rawValue, UriKind.Absolute, out Uri? parsedAddress) &&
            parsedAddress.Scheme is "http" or "https")
        {
            serverAddress = parsedAddress;
            return true;
        }

        Console.Error.WriteLine(
            "FREEBOX_SERVER_ADDRESS must be an absolute http(s) URI, for example https://mafreebox.freebox.fr/.");
        return false;
    }
}
