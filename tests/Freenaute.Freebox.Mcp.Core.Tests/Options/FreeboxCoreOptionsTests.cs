namespace Freenaute.Freebox.Mcp.Core.Tests.Options;

public sealed class FreeboxCoreOptionsTests
{
    [Fact]
    public void Defaults_AreStableAndSafe()
    {
        var options = new FreeboxCoreOptions();
        Assert.Equal(new Uri("https://mafreebox.freebox.fr/"), options.ServerAddress);
        Assert.Equal("org.freenaute.freebox-mcp", options.ApplicationId);
        Assert.Equal("Freenaute Freebox MCP", options.ApplicationName);
        Assert.Equal("1.0.0", options.ApplicationVersion);
        Assert.Equal(Environment.MachineName, options.DeviceName);
        Assert.Equal("default", options.CredentialProfile);
        Assert.Equal(TimeSpan.FromSeconds(30), options.RequestTimeout);
        Assert.True(options.EnableMdnsDiscovery);
        Assert.Equal(TimeSpan.FromSeconds(2), options.MdnsTimeout);
        Assert.True(options.EnableResilience);
    }

    [Fact]
    public void ApplicationId_UsesFreenauteOrgIdentifier()
    {
        string applicationId = new FreeboxCoreOptions().ApplicationId;

        Assert.Equal("org.freenaute.freebox-mcp", applicationId);
        Assert.DoesNotContain("io.github", applicationId, StringComparison.Ordinal);
    }

    [Fact]
    public void ServerAddress_IsSettableToCustomFreeboxAddress()
    {
        var options = new FreeboxCoreOptions();
        var custom = new Uri("http://192.168.1.254:8080/");

        options.ServerAddress = custom;

        Assert.Equal(custom, options.ServerAddress);
    }
}
