namespace Freenaute.Freebox.Mcp.Core.Repositories;

public interface IFreeboxDataRepository
{
    ValueTask<ApiVersionResponse> GetApiVersionAsync(CancellationToken cancellationToken = default);

    ValueTask<ConnectionStatus> GetConnectionStatusAsync(CancellationToken cancellationToken = default);

    ValueTask<ConnectionConfiguration> GetConnectionConfigurationAsync(CancellationToken cancellationToken = default);

    ValueTask<LanConfig> GetLanConfigurationAsync(CancellationToken cancellationToken = default);

    ValueTask<LanBrowserInterface[]> GetLanInterfacesAsync(CancellationToken cancellationToken = default);

    ValueTask<LanHost[]> GetLanHostsAsync(string interfaceName, CancellationToken cancellationToken = default);

    ValueTask<WifiGlobalConfig> GetWifiConfigurationAsync(CancellationToken cancellationToken = default);

    ValueTask<ObjectOrArray<WifiGlobalState>> GetWifiStateAsync(CancellationToken cancellationToken = default);

    ValueTask<WifiAp[]> GetWifiAccessPointsAsync(CancellationToken cancellationToken = default);

    ValueTask<WifiBss[]> GetWifiBssListAsync(CancellationToken cancellationToken = default);

    ValueTask<SystemConfig> GetSystemConfigurationAsync(CancellationToken cancellationToken = default);

    ValueTask<ObjectOrArray<Download>> GetDownloadsAsync(CancellationToken cancellationToken = default);

    ValueTask<DownloadStats> GetDownloadStatisticsAsync(CancellationToken cancellationToken = default);

    ValueTask<DownloadConfiguration> GetDownloadConfigurationAsync(CancellationToken cancellationToken = default);

    ValueTask<ShareLink[]> GetShareLinksAsync(CancellationToken cancellationToken = default);

    ValueTask<CallAccount> GetCallAccountAsync(CancellationToken cancellationToken = default);

    ValueTask<CallEntry[]> GetCallLogAsync(CancellationToken cancellationToken = default);

    ValueTask<VoicemailEntry[]> GetVoicemailsAsync(CancellationToken cancellationToken = default);

    ValueTask<ContactEntry[]> GetContactsAsync(CancellationToken cancellationToken = default);

    ValueTask<SambaConfig> GetSambaConfigurationAsync(CancellationToken cancellationToken = default);

    ValueTask<AfpConfig> GetAfpConfigurationAsync(CancellationToken cancellationToken = default);

    ValueTask<Camera[]> GetCamerasAsync(CancellationToken cancellationToken = default);

    ValueTask<NotificationTarget[]> GetNotificationTargetsAsync(CancellationToken cancellationToken = default);

    ValueTask<AirMediaConfigResponse> GetAirMediaConfigurationAsync(CancellationToken cancellationToken = default);

    ValueTask<IReadOnlyList<AirMediaReceiverResponse>> GetAirMediaReceiversAsync(
        CancellationToken cancellationToken = default);

    ValueTask<ParentalFilterConfig> GetParentalConfigurationAsync(CancellationToken cancellationToken = default);

    ValueTask<ParentalFilter[]> GetParentalFiltersAsync(CancellationToken cancellationToken = default);
}
