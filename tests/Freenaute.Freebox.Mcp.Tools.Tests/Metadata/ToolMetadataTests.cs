namespace Freenaute.Freebox.Mcp.Tools.Tests.Metadata;

public sealed class ToolMetadataTests
{
    public static TheoryData<Type, string, string, bool, bool, bool, Type> ExpectedTools =>
    [
        (typeof(FreeboxAirMediaTools), "freebox_airmedia_status", nameof(FreeboxAirMediaTools.GetStatusAsync), true,
            true, false, typeof(AirMediaStatusToolResult)),
        (typeof(FreeboxAirMediaTools), "freebox_airmedia_receivers", nameof(FreeboxAirMediaTools.GetReceiversAsync),
            true, true, false, typeof(AirMediaReceiversToolResult)),
        (typeof(FreeboxCallTools), "freebox_calls_log", nameof(FreeboxCallTools.GetLogAsync), true, true,
            false, typeof(CallLogToolResult)),
        (typeof(FreeboxCallTools), "freebox_calls_account", nameof(FreeboxCallTools.GetAccountAsync), true, true,
            false, typeof(CallAccountToolResult)),
        (typeof(FreeboxCallTools), "freebox_calls_mark_all_read", nameof(FreeboxCallTools.MarkAllReadAsync), false,
            true, false, typeof(CallMutationToolResult)),
        (typeof(FreeboxCallTools), "freebox_calls_clear_log", nameof(FreeboxCallTools.ClearLogAsync), false,
            true, true, typeof(CallMutationToolResult)),
        (typeof(FreeboxCamerasTools), "freebox_cameras_overview", nameof(FreeboxCamerasTools.GetCamerasAsync), true,
            true, false, typeof(CamerasToolResult)),
        (typeof(FreeboxCamerasTools), "freebox_home_status", nameof(FreeboxCamerasTools.GetHomeStatusAsync), true,
            true, false, typeof(HomeStatusToolResult)),
        (typeof(FreeboxContactsTools), "freebox_contacts_list", nameof(FreeboxContactsTools.ListAsync), true, true,
            false, typeof(ContactsToolResult)),
        (typeof(FreeboxContactsTools), "freebox_contacts_search", nameof(FreeboxContactsTools.SearchAsync), true,
            true, false, typeof(ContactsToolResult)),
        (typeof(FreeboxDiscoveryTools), "freebox_discover", nameof(FreeboxDiscoveryTools.DiscoverAsync), true, true,
            false, typeof(DiscoveryToolResult)),
        (typeof(FreeboxDownloadsTools), "freebox_downloads_list", nameof(FreeboxDownloadsTools.ListAsync), true, true,
            false, typeof(DownloadsToolResult)),
        (typeof(FreeboxDownloadsTools), "freebox_download_status", nameof(FreeboxDownloadsTools.GetAsync), true, true,
            false, typeof(DownloadToolResult)),
        (typeof(FreeboxDownloadsTools), "freebox_download_stats", nameof(FreeboxDownloadsTools.GetStatsAsync), true,
            true, false, typeof(DownloadStatsToolResult)),
        (typeof(FreeboxDownloadsTools), "freebox_download_configuration",
            nameof(FreeboxDownloadsTools.GetConfigurationAsync), true, true, false,
            typeof(DownloadConfigurationToolResult)),
        (typeof(FreeboxDownloadsTools), "freebox_download_files", nameof(FreeboxDownloadsTools.GetFilesAsync), true,
            true, false, typeof(DownloadFilesToolResult)),
        (typeof(FreeboxDownloadsTools), "freebox_download_log", nameof(FreeboxDownloadsTools.GetLogAsync), true, true,
            false, typeof(DownloadLogToolResult)),
        (typeof(FreeboxDownloadsTools), "freebox_download_pieces", nameof(FreeboxDownloadsTools.GetPiecesAsync), true,
            true, false, typeof(DownloadPiecesToolResult)),
        (typeof(FreeboxPairingTools), "freebox_pairing_status", nameof(FreeboxPairingTools.GetStatusAsync), true, true,
            false, typeof(PairingStatusToolResult)),
        (typeof(FreeboxPairingTools), "freebox_pairing_initiate", nameof(FreeboxPairingTools.InitiateAsync), false,
            false, false, typeof(PairingInitiationToolResult)),
        (typeof(FreeboxPairingTools), "freebox_pairing_wait", nameof(FreeboxPairingTools.WaitAsync), false,
            true, false, typeof(PairingApprovalToolResult)),
        (typeof(FreeboxNetworkTools), "freebox_connection_status", nameof(FreeboxNetworkTools.GetConnectionAsync), true,
            true, false, typeof(ConnectionToolResult)),
        (typeof(FreeboxNetworkTools), "freebox_lan_overview", nameof(FreeboxNetworkTools.GetLanAsync), true, true,
            false, typeof(LanToolResult)),
        (typeof(FreeboxNetworkTools), "freebox_wifi_status", nameof(FreeboxNetworkTools.GetWifiAsync), true, true,
            false, typeof(WifiToolResult)),
        (typeof(FreeboxNotificationTools), "freebox_status_feed",
            nameof(FreeboxNotificationTools.GetStatusFeedAsync), true, true, false, typeof(StatusFeedToolResult)),
        (typeof(FreeboxNotificationTools), "freebox_notifications_feed",
            nameof(FreeboxNotificationTools.GetNotificationsFeedAsync), true, true, false,
            typeof(NotificationFeedToolResult)),
        (typeof(FreeboxParentalTools), "freebox_parental_config",
            nameof(FreeboxParentalTools.GetConfigAsync), true, true, false,
            typeof(ParentalConfigToolResult)),
        (typeof(FreeboxParentalTools), "freebox_parental_filters",
            nameof(FreeboxParentalTools.GetFiltersAsync), true, true, false,
            typeof(ParentalFiltersToolResult)),
        (typeof(FreeboxSharesTools), "freebox_nas_visibility", nameof(FreeboxSharesTools.GetNasVisibilityAsync), true,
            true, false, typeof(NasVisibilityToolResult)),
        (typeof(FreeboxSharesTools), "freebox_shares_visibility", nameof(FreeboxSharesTools.GetSharesVisibilityAsync),
            true, true, false, typeof(SharesVisibilityToolResult)),
        (typeof(FreeboxSystemTools), "freebox_system_status", nameof(FreeboxSystemTools.GetStatusAsync), true, true,
            false, typeof(SystemToolResult))
    ];

    [Theory]
    [MemberData(nameof(ExpectedTools))]
    public void ToolMethod_ExposesExpectedSafeMcpMetadata(
        Type owner,
        string name,
        string methodName,
        bool readOnly,
        bool idempotent,
        bool destructive,
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
        Assert.Equal(destructive, tool.Destructive);
        Assert.True(tool.OpenWorld);
        Assert.True(tool.UseStructuredContent);
        Assert.Equal(outputSchema, tool.OutputSchemaType);
        Assert.False(string.IsNullOrWhiteSpace(description.Description));
    }

    [Fact]
    public void ToolsAssembly_ExposesExactlyTheExpectedToolNames()
    {
        string[] discovered =
        [
            .. typeof(FreeboxDiscoveryTools).Assembly.GetTypes()
                .Where(type => type.GetCustomAttributes<McpServerToolTypeAttribute>().Any())
                .SelectMany(type => type.GetMethods(
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
                .Select(method => method.GetCustomAttributes<McpServerToolAttribute>().FirstOrDefault()?.Name)
                .Where(name => name is not null)
                .Select(name => name!)
                .Order()
        ];

        string[] expected =
        [
            "freebox_airmedia_receivers",
            "freebox_airmedia_status",
            "freebox_calls_account",
            "freebox_calls_clear_log",
            "freebox_calls_log",
            "freebox_calls_mark_all_read",
            "freebox_cameras_overview",
            "freebox_connection_status",
            "freebox_contacts_list",
            "freebox_contacts_search",
            "freebox_discover",
            "freebox_download_configuration",
            "freebox_download_files",
            "freebox_download_log",
            "freebox_download_pieces",
            "freebox_download_stats",
            "freebox_download_status",
            "freebox_downloads_list",
            "freebox_home_status",
            "freebox_lan_overview",
            "freebox_nas_visibility",
            "freebox_notifications_feed",
            "freebox_parental_config",
            "freebox_parental_filters",
            "freebox_pairing_initiate",
            "freebox_pairing_status",
            "freebox_pairing_wait",
            "freebox_shares_visibility",
            "freebox_status_feed",
            "freebox_system_status",
            "freebox_wifi_status"
        ];
        Assert.Equal(expected.Order().ToArray(), discovered);
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
