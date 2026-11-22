namespace Freenaute.Freebox.Mcp.Core.Configuration;

public sealed class FreeboxCoreOptions
{
    public Uri ServerAddress { get; set; } = new("https://mafreebox.freebox.fr/");
    public string ApplicationId { get; set; } = "org.freenaute.freebox-mcp";
    public string ApplicationName { get; set; } = "Freenaute Freebox MCP";
    public string ApplicationVersion { get; set; } = "1.0.0";
    public string DeviceName { get; set; } = Environment.MachineName;
    public string CredentialProfile { get; set; } = "default";
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public bool EnableMdnsDiscovery { get; set; } = true;
    public TimeSpan MdnsTimeout { get; set; } = TimeSpan.FromSeconds(2);
    public bool EnableResilience { get; set; } = true;
    public bool AllowPairingInitiation { get; set; }
    public TimeSpan PairingPollInterval { get; set; } = TimeSpan.FromSeconds(2);
    public TimeSpan PairingWaitTimeout { get; set; } = TimeSpan.FromMinutes(10);
    public bool AuthenticateOnStartup { get; set; } = true;
}
