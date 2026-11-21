namespace Freenaute.Freebox.Mcp.Tools.Tests.Metadata;

public sealed class ToolMetadataTests
{
    public static TheoryData<Type, string, string, bool, bool, Type> ExpectedTools =>
    [
        (typeof(FreeboxDiscoveryTools), "freebox_discover", nameof(FreeboxDiscoveryTools.DiscoverAsync), true, true,
            typeof(DiscoveryToolResult)),
        (typeof(FreeboxPairingTools), "freebox_pairing_status", nameof(FreeboxPairingTools.GetStatusAsync), true, true,
            typeof(PairingStatusToolResult)),
        (typeof(FreeboxPairingTools), "freebox_pairing_initiate", nameof(FreeboxPairingTools.InitiateAsync), false,
            false, typeof(PairingInitiationToolResult)),
        (typeof(FreeboxPairingTools), "freebox_pairing_wait", nameof(FreeboxPairingTools.WaitAsync), false,
            true, typeof(PairingApprovalToolResult)),
        (typeof(FreeboxNetworkTools), "freebox_connection_status", nameof(FreeboxNetworkTools.GetConnectionAsync), true,
            true, typeof(ConnectionToolResult)),
        (typeof(FreeboxNetworkTools), "freebox_lan_overview", nameof(FreeboxNetworkTools.GetLanAsync), true, true,
            typeof(LanToolResult)),
        (typeof(FreeboxNetworkTools), "freebox_wifi_status", nameof(FreeboxNetworkTools.GetWifiAsync), true, true,
            typeof(WifiToolResult)),
        (typeof(FreeboxSystemTools), "freebox_system_status", nameof(FreeboxSystemTools.GetStatusAsync), true, true,
            typeof(SystemToolResult))
    ];

    [Theory]
    [MemberData(nameof(ExpectedTools))]
    public void ToolMethod_ExposesExpectedSafeMcpMetadata(
        Type owner,
        string name,
        string methodName,
        bool readOnly,
        bool idempotent,
        Type outputSchema)
    {
        Assert.NotEmpty(owner.GetCustomAttributes<McpServerToolTypeAttribute>());
        MethodInfo method = owner.GetMethod(methodName)!;
        McpServerToolAttribute tool = Assert.Single(method.GetCustomAttributes<McpServerToolAttribute>());
        DescriptionAttribute description = Assert.Single(method.GetCustomAttributes<DescriptionAttribute>());

        Assert.Equal(name, tool.Name);
        Assert.False(string.IsNullOrWhiteSpace(tool.Title));
        Assert.Equal(readOnly, tool.ReadOnly);
        Assert.Equal(idempotent, tool.Idempotent);
        Assert.False(tool.Destructive);
        Assert.True(tool.OpenWorld);
        Assert.True(tool.UseStructuredContent);
        Assert.Equal(outputSchema, tool.OutputSchemaType);
        Assert.False(string.IsNullOrWhiteSpace(description.Description));
    }

    [Fact]
    public void ToolsAssembly_ExposesExactlyTheEightExpectedToolNames()
    {
        string[] discovered = typeof(FreeboxDiscoveryTools).Assembly.GetTypes()
            .Where(type => type.GetCustomAttributes<McpServerToolTypeAttribute>().Any())
            .SelectMany(type => type.GetMethods(
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            .Select(method => method.GetCustomAttributes<McpServerToolAttribute>().FirstOrDefault()?.Name)
            .Where(name => name is not null)
            .Select(name => name!)
            .Order()
            .ToArray();

        Assert.Equal(
        [
            "freebox_connection_status",
            "freebox_discover",
            "freebox_lan_overview",
            "freebox_pairing_initiate",
            "freebox_pairing_status",
            "freebox_pairing_wait",
            "freebox_system_status",
            "freebox_wifi_status"
        ], discovered);
    }

    [Fact]
    public void GetLanAsync_ParametersExposeDescriptionsAndSafeDefaults()
    {
        ParameterInfo[] parameters = typeof(FreeboxNetworkTools)
            .GetMethod(nameof(FreeboxNetworkTools.GetLanAsync))!.GetParameters();
        Assert.Equal(3, parameters.Length);
        Assert.False(Assert.IsType<bool>(parameters[0].DefaultValue));
        Assert.Empty(parameters[0].GetCustomAttributes<DescriptionAttribute>());
        Assert.Null(parameters[1].DefaultValue);
        Assert.Empty(parameters[1].GetCustomAttributes<DescriptionAttribute>());
        Assert.Equal(typeof(CancellationToken), parameters[2].ParameterType);
    }
}
