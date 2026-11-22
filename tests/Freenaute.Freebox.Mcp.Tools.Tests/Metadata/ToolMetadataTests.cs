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
        (typeof(FreeboxFilesTools), "freebox_files_list", nameof(FreeboxFilesTools.ListAsync), true, true, false,
            typeof(FilesListToolResult)),
        (typeof(FreeboxFilesTools), "freebox_files_info", nameof(FreeboxFilesTools.GetInfoAsync), true, true, false,
            typeof(FileInfoToolResult)),
        (typeof(FreeboxFilesTools), "freebox_files_tasks", nameof(FreeboxFilesTools.GetTasksAsync), true, true,
            false, typeof(FilesTasksToolResult)),
        (typeof(FreeboxFilesTools), "freebox_files_mkdir", nameof(FreeboxFilesTools.CreateDirectoryAsync), false,
            true, false, typeof(FilesMutationToolResult)),
        (typeof(FreeboxFilesTools), "freebox_files_move", nameof(FreeboxFilesTools.MoveAsync), false, true, false,
            typeof(FileTaskToolResult)),
        (typeof(FreeboxFilesTools), "freebox_files_delete", nameof(FreeboxFilesTools.DeleteAsync), false, true, true,
            typeof(FileTaskToolResult)),
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
        (typeof(FreeboxAdvancedNetworkTools), "freebox_network_dhcp_leases",
            nameof(FreeboxAdvancedNetworkTools.GetDhcpLeasesAsync), true, true, false,
            typeof(DhcpLeasesToolResult)),
        (typeof(FreeboxAdvancedNetworkTools), "freebox_network_port_forwards",
            nameof(FreeboxAdvancedNetworkTools.GetPortForwardsAsync), true, true, false,
            typeof(PortForwardsToolResult)),
        (typeof(FreeboxAdvancedNetworkTools), "freebox_network_ipv6_status",
            nameof(FreeboxAdvancedNetworkTools.GetIpv6StatusAsync), true, true, false,
            typeof(Ipv6StatusToolResult)),
        (typeof(FreeboxAdvancedNetworkTools), "freebox_network_port_forward_create",
            nameof(FreeboxAdvancedNetworkTools.CreatePortForwardAsync), false, false, false,
            typeof(PortForwardMutationToolResult)),
        (typeof(FreeboxAdvancedNetworkTools), "freebox_network_port_forward_update",
            nameof(FreeboxAdvancedNetworkTools.UpdatePortForwardAsync), false, true, false,
            typeof(PortForwardMutationToolResult)),
        (typeof(FreeboxAdvancedNetworkTools), "freebox_network_port_forward_delete",
            nameof(FreeboxAdvancedNetworkTools.DeletePortForwardAsync), false, true, true,
            typeof(PortForwardMutationToolResult)),
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
        (typeof(FreeboxParentalTools), "freebox_parental_filter_create",
            nameof(FreeboxParentalTools.CreateFilterAsync), false, false, false,
            typeof(ParentalMutationToolResult)),
        (typeof(FreeboxParentalTools), "freebox_parental_filter_update",
            nameof(FreeboxParentalTools.UpdateFilterAsync), false, true, false,
            typeof(ParentalMutationToolResult)),
        (typeof(FreeboxParentalTools), "freebox_parental_filter_delete",
            nameof(FreeboxParentalTools.DeleteFilterAsync), false, true, true,
            typeof(ParentalMutationToolResult)),
        (typeof(FreeboxParentalTools), "freebox_parental_filter_planning_update",
            nameof(FreeboxParentalTools.UpdatePlanningAsync), false, true, false,
            typeof(ParentalMutationToolResult)),
        (typeof(FreeboxParentalTools), "freebox_parental_config_update",
            nameof(FreeboxParentalTools.UpdateConfigAsync), false, true, false,
            typeof(ParentalMutationToolResult)),
        (typeof(FreeboxSharesTools), "freebox_nas_visibility", nameof(FreeboxSharesTools.GetNasVisibilityAsync), true,
            true, false, typeof(NasVisibilityToolResult)),
        (typeof(FreeboxSharesTools), "freebox_shares_visibility", nameof(FreeboxSharesTools.GetSharesVisibilityAsync),
            true, true, false, typeof(SharesVisibilityToolResult)),
        (typeof(FreeboxServicesTools), "freebox_services_status", nameof(FreeboxServicesTools.GetStatusAsync),
            true, true, false, typeof(ServicesStatusToolResult)),
        (typeof(FreeboxServicesTools), "freebox_services_ftp_update", nameof(FreeboxServicesTools.UpdateFtpAsync),
            false, true, false, typeof(ServiceMutationToolResult)),
        (typeof(FreeboxServicesTools), "freebox_services_tftp_update", nameof(FreeboxServicesTools.UpdateTftpAsync),
            false, true, false, typeof(ServiceMutationToolResult)),
        (typeof(FreeboxServicesTools), "freebox_services_upnpav_update", nameof(FreeboxServicesTools.UpdateUPnpAvAsync),
            false, true, false, typeof(ServiceMutationToolResult)),
        (typeof(FreeboxServicesTools), "freebox_services_samba_update", nameof(FreeboxServicesTools.UpdateSambaAsync),
            false, true, false, typeof(ServiceMutationToolResult)),
        (typeof(FreeboxServicesTools), "freebox_services_afp_update", nameof(FreeboxServicesTools.UpdateAfpAsync),
            false, true, false, typeof(ServiceMutationToolResult)),
        (typeof(FreeboxSystemTools), "freebox_system_status", nameof(FreeboxSystemTools.GetStatusAsync), true, true,
            false, typeof(SystemToolResult)),
        (typeof(FreeboxSystemHomeTools), "freebox_system_updates_status",
            nameof(FreeboxSystemHomeTools.GetUpdatesStatusAsync), true, true, false,
            typeof(SystemUpdatesStatusToolResult)),
        (typeof(FreeboxSystemHomeTools), "freebox_system_standby_status",
            nameof(FreeboxSystemHomeTools.GetStandbyStatusAsync), true, true, false,
            typeof(StandbyStatusToolResult)),
        (typeof(FreeboxSystemHomeTools), "freebox_system_standby_configure",
            nameof(FreeboxSystemHomeTools.ConfigureStandbyAsync), false, true, false,
            typeof(StandbyConfigurationToolResult))
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
            "freebox_files_delete",
            "freebox_files_info",
            "freebox_files_list",
            "freebox_files_mkdir",
            "freebox_files_move",
            "freebox_files_tasks",
            "freebox_home_status",
            "freebox_network_dhcp_leases",
            "freebox_network_ipv6_status",
            "freebox_network_port_forward_create",
            "freebox_network_port_forward_delete",
            "freebox_network_port_forward_update",
            "freebox_network_port_forwards",
            "freebox_lan_overview",
            "freebox_nas_visibility",
            "freebox_notifications_feed",
            "freebox_parental_config",
            "freebox_parental_config_update",
            "freebox_parental_filter_create",
            "freebox_parental_filter_delete",
            "freebox_parental_filter_planning_update",
            "freebox_parental_filter_update",
            "freebox_parental_filters",
            "freebox_pairing_initiate",
            "freebox_pairing_status",
            "freebox_pairing_wait",
            "freebox_shares_visibility",
            "freebox_services_afp_update",
            "freebox_services_ftp_update",
            "freebox_services_samba_update",
            "freebox_services_status",
            "freebox_services_tftp_update",
            "freebox_services_upnpav_update",
            "freebox_status_feed",
            "freebox_system_status",
            "freebox_system_standby_configure",
            "freebox_system_standby_status",
            "freebox_system_updates_status",
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

    [Fact]
    public void DeletePortForwardAsync_RequiresExplicitConfirmation()
    {
        ParameterInfo confirm = typeof(FreeboxAdvancedNetworkTools)
            .GetMethod(nameof(FreeboxAdvancedNetworkTools.DeletePortForwardAsync))!
            .GetParameters()[1];

        Assert.Equal(typeof(bool), confirm.ParameterType);
        Assert.Equal(DBNull.Value, confirm.DefaultValue);
        Assert.NotEmpty(confirm.GetCustomAttributes<DescriptionAttribute>());
    }
}
