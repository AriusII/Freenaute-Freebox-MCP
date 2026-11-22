namespace Freenaute.Freebox.Mcp.Hosting;

internal sealed record McpStartupOptions(
    string ApplicationId,
    string ApplicationName,
    string DeviceName,
    string CredentialProfile,
    Uri? ServerAddress,
    int? PairingPollIntervalSeconds,
    int? PairingWaitTimeoutSeconds,
    bool? AuthenticateOnStartup,
    bool AllowPairingInitiation);
