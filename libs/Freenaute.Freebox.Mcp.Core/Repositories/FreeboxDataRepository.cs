namespace Freenaute.Freebox.Mcp.Core.Repositories;

public sealed class FreeboxDataRepository(IFreeboxClient client) : IFreeboxDataRepository
{
    private readonly IFreeboxClient _client = client ?? throw new ArgumentNullException(nameof(client));

    public async ValueTask<ApiVersionResponse> GetApiVersionAsync(CancellationToken cancellationToken = default)
    {
        return await _client.Discovery.GetApiVersionAsync(cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<ConnectionStatus> GetConnectionStatusAsync(CancellationToken cancellationToken = default)
    {
        return await _client.Network.Connection.GetStatusAsync(cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<ConnectionConfiguration> GetConnectionConfigurationAsync(
        CancellationToken cancellationToken = default)
    {
        return await _client.Network.Connection.GetConfigurationAsync(cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<LanConfig> GetLanConfigurationAsync(CancellationToken cancellationToken = default)
    {
        return await _client.Network.Lan.GetConfigurationAsync(cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<LanBrowserInterface[]> GetLanInterfacesAsync(CancellationToken cancellationToken = default)
    {
        return await _client.Network.Lan.GetInterfacesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<LanHost[]> GetLanHostsAsync(
        string interfaceName,
        CancellationToken cancellationToken = default)
    {
        return await _client.Network.Lan.Interface(interfaceName).GetHostsAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async ValueTask<WifiGlobalConfig> GetWifiConfigurationAsync(CancellationToken cancellationToken = default)
    {
        return await _client.Network.Wifi.GetConfigurationAsync(cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<ObjectOrArray<WifiGlobalState>> GetWifiStateAsync(
        CancellationToken cancellationToken = default)
    {
        return await _client.Network.Wifi.GetStateAsync(cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<WifiAp[]> GetWifiAccessPointsAsync(CancellationToken cancellationToken = default)
    {
        return await _client.Network.Wifi.GetAccessPointsAsync(cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<WifiBss[]> GetWifiBssListAsync(CancellationToken cancellationToken = default)
    {
        return await _client.Network.Wifi.GetBssListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<SystemConfig> GetSystemConfigurationAsync(CancellationToken cancellationToken = default)
    {
        return await _client.SystemHome.System.GetAsync(cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<ObjectOrArray<Download>> GetDownloadsAsync(CancellationToken cancellationToken = default)
    {
        return await _client.Files.Downloads.GetAllAsync(cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<DownloadStats> GetDownloadStatisticsAsync(CancellationToken cancellationToken = default)
    {
        return await _client.Files.Downloads.GetStatisticsAsync(cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<DownloadConfiguration> GetDownloadConfigurationAsync(
        CancellationToken cancellationToken = default)
    {
        return await _client.Files.Downloads.GetConfigurationAsync(cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<ShareLink[]> GetShareLinksAsync(CancellationToken cancellationToken = default)
    {
        return await _client.Files.Shares.GetAllAsync(cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<CallAccount> GetCallAccountAsync(CancellationToken cancellationToken = default)
    {
        return await _client.Services.Call.GetAccountAsync(cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<CallEntry[]> GetCallLogAsync(CancellationToken cancellationToken = default)
    {
        return await _client.Services.Call.GetLogAsync(cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<VoicemailEntry[]> GetVoicemailsAsync(CancellationToken cancellationToken = default)
    {
        return await _client.Services.Call.GetVoicemailsAsync(cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<ContactEntry[]> GetContactsAsync(CancellationToken cancellationToken = default)
    {
        return await _client.Services.Contacts.GetAllAsync(cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<SambaConfig> GetSambaConfigurationAsync(CancellationToken cancellationToken = default)
    {
        return await _client.Services.NetworkShare.GetSambaConfigurationAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async ValueTask<AfpConfig> GetAfpConfigurationAsync(CancellationToken cancellationToken = default)
    {
        return await _client.Services.NetworkShare.GetAfpConfigurationAsync(cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<Camera[]> GetCamerasAsync(CancellationToken cancellationToken = default)
    {
        return await _client.Cameras.ListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<NotificationTarget[]> GetNotificationTargetsAsync(
        CancellationToken cancellationToken = default)
    {
        return await _client.Notifications.ListTargetsAsync(cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<AirMediaConfigResponse> GetAirMediaConfigurationAsync(
        CancellationToken cancellationToken = default)
    {
        return await _client.AirMedia.GetConfigurationAsync(cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<IReadOnlyList<AirMediaReceiverResponse>> GetAirMediaReceiversAsync(
        CancellationToken cancellationToken = default)
    {
        return await _client.AirMedia.GetReceiversAsync(cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<ParentalFilterConfig> GetParentalConfigurationAsync(
        CancellationToken cancellationToken = default)
    {
        return await _client.Parental.GetConfigAsync(cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<ParentalFilter[]> GetParentalFiltersAsync(CancellationToken cancellationToken = default)
    {
        return await _client.Parental.GetFiltersAsync(cancellationToken).ConfigureAwait(false);
    }
}
