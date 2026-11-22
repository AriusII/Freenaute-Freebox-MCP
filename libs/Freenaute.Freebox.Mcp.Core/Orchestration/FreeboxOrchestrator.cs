namespace Freenaute.Freebox.Mcp.Core.Orchestration;

public sealed class FreeboxOrchestrator(
    IFreeboxDataRepository dataRepository,
    IFreeboxAuthenticationRepository authenticationRepository,
    IFreeboxCredentialStore credentialStore,
    IOptions<FreeboxCoreOptions> options) : IFreeboxOrchestrator
{
    private readonly bool _allowPairingInitiation = options?.Value.AllowPairingInitiation
                                                    ?? throw new ArgumentNullException(nameof(options));

    private readonly string _applicationId = options?.Value.ApplicationId
                                             ?? throw new ArgumentNullException(nameof(options));

    private readonly IFreeboxAuthenticationRepository _authenticationRepository =
        authenticationRepository ?? throw new ArgumentNullException(nameof(authenticationRepository));

    private readonly string _credentialProfile = options.Value.CredentialProfile;

    private readonly IFreeboxCredentialStore _credentialStore =
        credentialStore ?? throw new ArgumentNullException(nameof(credentialStore));

    private readonly IFreeboxDataRepository _dataRepository =
        dataRepository ?? throw new ArgumentNullException(nameof(dataRepository));

    private readonly TimeSpan _defaultPairingPollInterval = options.Value.PairingPollInterval;
    private readonly TimeSpan _defaultPairingWaitTimeout = options.Value.PairingWaitTimeout;

    public async ValueTask<FreeboxCoreResult<FreeboxPairingResult>> BeginPairingAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!_allowPairingInitiation)
        {
            return FreeboxCoreResults.Failure<FreeboxPairingResult>(
                FreeboxErrorMapper.Map(
                    new InvalidOperationException("Pairing initiation is disabled by host policy.")));
        }

        if (!_credentialStore.IsAvailable)
        {
            return CredentialStoreUnavailable<FreeboxPairingResult>();
        }

        try
        {
            FreeboxApprovalRegistration registration =
                await _authenticationRepository.BeginPairingAsync(cancellationToken).ConfigureAwait(false);

            return FreeboxCoreResults.Success(new FreeboxPairingResult(Map(registration.Status)));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            return FreeboxCoreResults.Failure<FreeboxPairingResult>(FreeboxErrorMapper.Map(exception));
        }
    }

    public async ValueTask<FreeboxCoreResult<FreeboxCredentialResult>> WaitForApprovalAsync(
        TimeSpan? pollInterval = null,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!_credentialStore.IsAvailable)
        {
            return CredentialStoreUnavailable<FreeboxCredentialResult>();
        }

        TimeSpan effectivePollInterval = pollInterval ?? _defaultPairingPollInterval;
        TimeSpan effectiveTimeout = timeout ?? _defaultPairingWaitTimeout;
        if (effectivePollInterval <= TimeSpan.Zero)
        {
            return FreeboxCoreResults.Failure<FreeboxCredentialResult>(FreeboxErrorMapper.Map(
                new ArgumentOutOfRangeException(nameof(pollInterval), "pollInterval must be strictly positive.")));
        }

        if (effectiveTimeout <= TimeSpan.Zero)
        {
            return FreeboxCoreResults.Failure<FreeboxCredentialResult>(FreeboxErrorMapper.Map(
                new ArgumentOutOfRangeException(nameof(timeout), "timeout must be strictly positive.")));
        }

        using var timeoutCts = new CancellationTokenSource(effectiveTimeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            timeoutCts.Token);

        try
        {
            FreeboxAppCredentials credentials = await _authenticationRepository
                .WaitForApprovalAsync(effectivePollInterval, linkedCts.Token)
                .ConfigureAwait(false);

            return FreeboxCoreResults.Success(Map(credentials));
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested &&
                                                 !cancellationToken.IsCancellationRequested)
        {
            return FreeboxCoreResults.Success(new FreeboxCredentialResult(FreeboxCredentialState.Expired));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (FreeboxAuthorizationException exception) when (exception.Status == AuthorizationTrackEnum.Denied)
        {
            return FreeboxCoreResults.Success(new FreeboxCredentialResult(FreeboxCredentialState.Denied));
        }
        catch (FreeboxAuthorizationException exception) when (exception.Status == AuthorizationTrackEnum.Timeout)
        {
            return FreeboxCoreResults.Success(new FreeboxCredentialResult(FreeboxCredentialState.Expired));
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            return FreeboxCoreResults.Failure<FreeboxCredentialResult>(FreeboxErrorMapper.Map(exception));
        }
    }

    public async ValueTask<FreeboxCoreResult<FreeboxConnectionResult>> GetConnectionStatusAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!_credentialStore.IsAvailable)
        {
            return FreeboxCoreResults.Success(new FreeboxConnectionResult(
                _credentialStore.Kind,
                false,
                FreeboxCredentialState.Unavailable));
        }

        try
        {
            FreeboxCredentialId id = await GetCredentialIdAsync(cancellationToken).ConfigureAwait(false);
            FreeboxAppCredentials? credentials = await _credentialStore.GetAsync(id, cancellationToken)
                .ConfigureAwait(false);

            return FreeboxCoreResults.Success(new FreeboxConnectionResult(
                _credentialStore.Kind,
                true,
                credentials is null ? FreeboxCredentialState.Missing : Map(credentials.Status)));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (InvalidDataException)
        {
            return FreeboxCoreResults.Success(new FreeboxConnectionResult(
                _credentialStore.Kind,
                true,
                FreeboxCredentialState.Corrupt));
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            return FreeboxCoreResults.Failure<FreeboxConnectionResult>(FreeboxErrorMapper.Map(exception));
        }
    }

    public async ValueTask<FreeboxCoreResult<bool>> ForgetCredentialsAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!_credentialStore.IsAvailable)
        {
            return CredentialStoreUnavailable<bool>();
        }

        try
        {
            await _authenticationRepository.ForgetAsync(cancellationToken).ConfigureAwait(false);
            return FreeboxCoreResults.Success(true);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            return FreeboxCoreResults.Failure<bool>(FreeboxErrorMapper.Map(exception));
        }
    }

    public async ValueTask<FreeboxCoreResult<FreeboxDiscoverySnapshot>> DiscoverAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            ApiVersionResponse response = await _dataRepository
                .GetApiVersionAsync(cancellationToken).ConfigureAwait(false);
            return FreeboxCoreResults.Success(new FreeboxDiscoverySnapshot(
                response.BoxModelName ?? response.BoxModel,
                response.ApiVersion,
                response.HttpsAvailable == true,
                response.HttpsPort));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            return FreeboxCoreResults.Failure<FreeboxDiscoverySnapshot>(FreeboxErrorMapper.Map(exception));
        }
    }

    public async ValueTask<FreeboxCoreResult<FreeboxNetworkSnapshot>> GetNetworkSnapshotAsync(
        bool includeHosts = false,
        string? interfaceName = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            Task<ConnectionStatus> connectionTask =
                _dataRepository.GetConnectionStatusAsync(cancellationToken).AsTask();
            Task<ConnectionConfiguration> connectionConfigurationTask =
                _dataRepository.GetConnectionConfigurationAsync(cancellationToken).AsTask();
            Task<LanConfig> lanConfigurationTask = _dataRepository.GetLanConfigurationAsync(cancellationToken).AsTask();
            Task<LanBrowserInterface[]> interfacesTask =
                _dataRepository.GetLanInterfacesAsync(cancellationToken).AsTask();
            Task<WifiGlobalConfig> wifiConfigurationTask =
                _dataRepository.GetWifiConfigurationAsync(cancellationToken).AsTask();
            Task<ObjectOrArray<WifiGlobalState>> wifiStateTask =
                _dataRepository.GetWifiStateAsync(cancellationToken).AsTask();
            Task<WifiAp[]> accessPointsTask = _dataRepository.GetWifiAccessPointsAsync(cancellationToken).AsTask();
            Task<WifiBss[]> networksTask = _dataRepository.GetWifiBssListAsync(cancellationToken).AsTask();

            await Task.WhenAll(connectionTask, connectionConfigurationTask, lanConfigurationTask, interfacesTask,
                wifiConfigurationTask, wifiStateTask, accessPointsTask, networksTask).ConfigureAwait(false);

            LanBrowserInterface[] interfaces = await interfacesTask.ConfigureAwait(false);
            if (interfaceName is not null && interfaces.All(item =>
                    !string.Equals(item.Name, interfaceName, StringComparison.Ordinal)))
            {
                throw new ArgumentException("interfaceName did not match an available LAN interface.",
                    nameof(interfaceName));
            }

            var hosts = new Dictionary<string, LanHost[]>(StringComparer.Ordinal);
            if (includeHosts)
            {
                foreach (LanBrowserInterface item in interfaces.Where(item =>
                             interfaceName is null ||
                             string.Equals(item.Name, interfaceName, StringComparison.Ordinal)))
                {
                    if (!string.IsNullOrWhiteSpace(item.Name))
                    {
                        hosts[item.Name] = await _dataRepository.GetLanHostsAsync(item.Name, cancellationToken)
                            .ConfigureAwait(false);
                    }
                }
            }

            ConnectionStatus connection = await connectionTask.ConfigureAwait(false);
            ConnectionConfiguration connectionConfiguration =
                await connectionConfigurationTask.ConfigureAwait(false);
            LanConfig lanConfiguration = await lanConfigurationTask.ConfigureAwait(false);
            WifiGlobalConfig wifiConfiguration = await wifiConfigurationTask.ConfigureAwait(false);
            ObjectOrArray<WifiGlobalState> wifiState = await wifiStateTask.ConfigureAwait(false);
            WifiAp[] accessPoints = await accessPointsTask.ConfigureAwait(false);
            WifiBss[] networks = await networksTask.ConfigureAwait(false);

            IReadOnlyDictionary<string, FreeboxLanHostSnapshot[]> mappedHosts = hosts.ToDictionary(
                pair => pair.Key,
                pair => pair.Value.Select(MapHost).ToArray(),
                StringComparer.Ordinal);
            return FreeboxCoreResults.Success(new FreeboxNetworkSnapshot(
                new FreeboxConnectionSnapshot(
                    connection.State,
                    connection.Type,
                    connection.Media,
                    connection.RateUp,
                    connection.RateDown,
                    connection.BandwidthUp,
                    connection.BandwidthDown,
                    connection.BytesUp,
                    connection.BytesDown,
                    connectionConfiguration.Ping,
                    connectionConfiguration.ApiRemoteAccess,
                    connectionConfiguration.RemoteAccess,
                    connectionConfiguration.Wol,
                    connectionConfiguration.SipAlg),
                new FreeboxLanSnapshot(
                    lanConfiguration.Name,
                    lanConfiguration.Type,
                    lanConfiguration.Mode,
                    [.. interfaces.Select(item => new FreeboxLanInterfaceSnapshot(item.Name, item.HostCount))],
                    mappedHosts),
                new FreeboxWifiSnapshot(
                    wifiConfiguration.Enabled,
                    wifiConfiguration.MacFilterState,
                    MapWifiState(wifiState),
                    [
                        .. accessPoints.Select(item => new FreeboxWifiAccessPointSnapshot(
                            item.Id, item.Status?.State))
                    ],
                    [.. networks.Select(MapWifiBss)])));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            return FreeboxCoreResults.Failure<FreeboxNetworkSnapshot>(FreeboxErrorMapper.Map(exception));
        }
    }

    public async ValueTask<FreeboxCoreResult<FreeboxSystemSnapshot>> GetSystemSnapshotAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            SystemConfig configuration = await _dataRepository
                .GetSystemConfigurationAsync(cancellationToken).ConfigureAwait(false);
            return FreeboxCoreResults.Success(new FreeboxSystemSnapshot(
                configuration.FirmwareVersion,
                configuration.ModelInfo?.PrettyName ?? configuration.ModelInfo?.Name,
                configuration.BoardName,
                configuration.UptimeVal,
                configuration.BoxAuthenticated,
                configuration.Usb3Enable,
                configuration.UserStoragePowered,
                configuration.DiskStatus?.ToString(),
                configuration.Sensors?.Select(item => new FreeboxSystemSensorSnapshot(item.Id, item.Value)).ToArray() ??
                [],
                configuration.Fans?.Select(item => new FreeboxSystemSensorSnapshot(item.Id, item.Value)).ToArray() ??
                [],
                configuration.Expansions?.Select(item => new FreeboxSystemExpansionSnapshot(
                    item.Slot, item.Present, item.Supported, item.Type?.ToString())).ToArray() ?? []));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            return FreeboxCoreResults.Failure<FreeboxSystemSnapshot>(FreeboxErrorMapper.Map(exception));
        }
    }

    private async ValueTask<FreeboxCredentialId> GetCredentialIdAsync(CancellationToken cancellationToken)
    {
        ApiVersionResponse apiVersion = await _dataRepository.GetApiVersionAsync(cancellationToken)
            .ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(apiVersion.Uid))
        {
            throw new InvalidOperationException("The Freebox discovery response did not contain a valid device UID.");
        }

        return new FreeboxCredentialId(_credentialProfile, _applicationId, apiVersion.Uid);
    }

    private static FreeboxCoreResult<T> CredentialStoreUnavailable<T>()
    {
        return FreeboxCoreResults.Failure<T>(FreeboxErrorMapper.Map(
            new NotSupportedException("Secure OS credential storage is unavailable on this platform.")));
    }

    private static FreeboxCredentialResult Map(FreeboxAppCredentials credentials)
    {
        return new FreeboxCredentialResult(
            Map(credentials.Status),
            credentials.CreatedAt,
            credentials.UpdatedAt);
    }

    private static FreeboxCredentialState Map(FreeboxCredentialStatus status)
    {
        return status switch
        {
            FreeboxCredentialStatus.Pending => FreeboxCredentialState.Pending,
            FreeboxCredentialStatus.Approved => FreeboxCredentialState.Approved,
            FreeboxCredentialStatus.Invalid => FreeboxCredentialState.Corrupt,
            _ => FreeboxCredentialState.Corrupt
        };
    }

    private static FreeboxLanHostSnapshot MapHost(LanHost host)
    {
        return new FreeboxLanHostSnapshot(
            host.Id,
            host.HostType,
            host.Reachable,
            host.Active,
            host.LastActivity,
            host.Interface);
    }

    private static string? MapWifiState(ObjectOrArray<WifiGlobalState> state)
    {
        return state.IsObject ? state.Object?.State : state.Items.Count > 0 ? state.Items[0].State : null;
    }

    private static FreeboxWifiBssSnapshot MapWifiBss(WifiBss bss)
    {
        string? id = bss.Id is null
            ? null
            : bss.Id.Value.Kind == StringOrIntegerKind.String
                ? bss.Id.Value.String
                : bss.Id.Value.Integer.ToString(CultureInfo.InvariantCulture);
        long? phyId = bss.PhyId is not null && bss.PhyId.Value.Kind == StringOrIntegerKind.Integer
            ? bss.PhyId.Value.Integer
            : null;
        return new FreeboxWifiBssSnapshot(
            id,
            phyId,
            bss.Status?.State,
            bss.Status?.StaCount,
            bss.Status?.AuthorizedStaCount,
            bss.Config?.Enabled,
            bss.Config?.Encryption,
            bss.Config?.WpsEnabled);
    }
}
