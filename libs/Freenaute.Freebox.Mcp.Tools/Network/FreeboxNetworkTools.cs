using Microsoft.Extensions.Logging.Abstractions;

namespace Freenaute.Freebox.Mcp.Tools.Network;

[McpServerToolType]
public sealed partial class FreeboxNetworkTools(
    IFreeboxOrchestrator orchestrator,
    ILogger<FreeboxNetworkTools>? logger = null)
{
    private const int MaxInterfaces = 32;
    private const int MaxHostsPerInterface = 128;
    private const int MaxAccessPoints = 32;
    private const int MaxBss = 128;

    private readonly ILogger<FreeboxNetworkTools> _logger =
        logger ?? NullLogger<FreeboxNetworkTools>.Instance;

    [McpServerTool(Name = "freebox_connection_status", Title = "Get Freebox connection status", ReadOnly = true,
        Idempotent = true, Destructive = false, OpenWorld = true, UseStructuredContent = true,
        OutputSchemaType = typeof(ConnectionToolResult))]
    [Description("Returns bounded connection state and traffic counters; network addresses are excluded.")]
    public async Task<ConnectionToolResult> GetConnectionAsync(CancellationToken cancellationToken)
    {
        LogConnectionStatusStarted(_logger);
        FreeboxCoreResult<FreeboxNetworkSnapshot> result = await orchestrator
            .GetNetworkSnapshotAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            LogConnectionStatusFailed(_logger, result.Error?.Code);
            return ToolResultFactory.Failure<ConnectionToolResult>(result.Error!);
        }

        FreeboxConnectionSnapshot value = result.Value!.Connection;
        LogConnectionStatusSucceeded(_logger, value.State, value.Media);
        return new ConnectionToolResult(
            Safe.Text(value.State, 32), Safe.Text(value.Type, 32), Safe.Text(value.Media, 32),
            value.RateUp, value.RateDown, value.BandwidthUp, value.BandwidthDown,
            value.BytesUp, value.BytesDown, value.PingEnabled, value.ApiRemoteAccessEnabled,
            value.RemoteAccessEnabled, value.WakeOnLanEnabled, Safe.Text(value.SipAlg, 32));
    }

    [McpServerTool(Name = "freebox_lan_overview", Title = "Get Freebox LAN overview", ReadOnly = true,
        Idempotent = true, Destructive = false, OpenWorld = true, UseStructuredContent = true,
        OutputSchemaType = typeof(LanToolResult))]
    [Description(
        "Returns bounded LAN summaries. Host identifiers are hashed; names, domains, and addresses are excluded.")]
    public async Task<LanToolResult> GetLanAsync(
        bool includeHosts = false,
        string? interfaceName = null,
        CancellationToken cancellationToken = default)
    {
        LogLanOverviewStarted(_logger, includeHosts, interfaceName);
        if (interfaceName is not null && (interfaceName.Length is < 1 or > 64 || interfaceName.Any(char.IsControl)))
        {
            throw new ArgumentException("interfaceName must be 1 to 64 characters and contain no control characters.",
                nameof(interfaceName));
        }

        FreeboxCoreResult<FreeboxNetworkSnapshot> result = await orchestrator
            .GetNetworkSnapshotAsync(includeHosts, interfaceName, cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            LogLanOverviewFailed(_logger, result.Error?.Code);
            return ToolResultFactory.Failure<LanToolResult>(result.Error!);
        }

        FreeboxLanSnapshot value = result.Value!.Lan;
        LanInterfaceToolResult[] summaries =
        [
            .. value.Interfaces
                .Where(item =>
                    interfaceName is null || string.Equals(item.Name, interfaceName, StringComparison.Ordinal))
                .Take(MaxInterfaces)
                .Select(item =>
                {
                    string name = item.Name is null ? string.Empty : Safe.Text(item.Name, 64);
                    LanHostToolResult[]? hosts = includeHosts && value.Hosts.TryGetValue(
                        item.Name ?? string.Empty, out FreeboxLanHostSnapshot[]? raw)
                        ? [.. raw.Take(MaxHostsPerInterface).Select(MapHost)]
                        : null;
                    return new LanInterfaceToolResult(name, item.HostCount, hosts);
                })
        ];

        LogLanOverviewSucceeded(_logger, summaries.Length, includeHosts);
        return new LanToolResult(
            Safe.Text(value.Name, 64),
            Safe.Text(value.Type, 32),
            Safe.Text(value.Mode, 32),
            summaries);
    }

    [McpServerTool(Name = "freebox_wifi_status", Title = "Get Freebox Wi-Fi status", ReadOnly = true, Idempotent = true,
        Destructive = false, OpenWorld = true, UseStructuredContent = true, OutputSchemaType = typeof(WifiToolResult))]
    [Description("Returns bounded sanitized Wi-Fi state. Host identifiers, BSSIDs, names, and SSIDs are excluded.")]
    public async Task<WifiToolResult> GetWifiAsync(CancellationToken cancellationToken)
    {
        LogWifiStatusStarted(_logger);
        FreeboxCoreResult<FreeboxNetworkSnapshot> result = await orchestrator
            .GetNetworkSnapshotAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            LogWifiStatusFailed(_logger, result.Error?.Code);
            return ToolResultFactory.Failure<WifiToolResult>(result.Error!);
        }

        FreeboxWifiSnapshot value = result.Value!.Wifi;
        LogWifiStatusSucceeded(_logger, value.Enabled, value.AccessPoints.Length, value.Networks.Length);
        return new WifiToolResult(value.Enabled, Safe.Text(value.MacFilterState, 32), Safe.Text(value.State, 32),
            [
                .. value.AccessPoints.Take(MaxAccessPoints).Select(item =>
                    new WifiAccessPointToolResult(item.Id, string.Empty, Safe.Text(item.State, 32)))
            ],
            [.. value.Networks.Take(MaxBss).Select(MapBss)]);
    }

    private static LanHostToolResult MapHost(FreeboxLanHostSnapshot host)
    {
        return new LanHostToolResult(Safe.StableIdentifier(host.Id), null, Safe.Text(host.HostType, 32), null,
            host.Reachable, host.Active, host.LastActivity, Safe.Text(host.Interface, 64));
    }

    private static WifiBssToolResult MapBss(FreeboxWifiBssSnapshot bss)
    {
        return new WifiBssToolResult(Safe.StableIdentifier(bss.Id), bss.PhyId,
            Safe.Text(bss.State, 32), bss.StationCount, bss.AuthorizedStationCount,
            bss.Enabled, Safe.Text(bss.Encryption, 32), bss.WpsEnabled);
    }

    [LoggerMessage(EventId = 1100, Level = LogLevel.Information, Message = "Executing freebox_connection_status.")]
    private static partial void LogConnectionStatusStarted(ILogger logger);

    [LoggerMessage(EventId = 1101, Level = LogLevel.Warning, Message = "freebox_connection_status failed: {ErrorCode}")]
    private static partial void LogConnectionStatusFailed(ILogger logger, FreeboxCoreErrorCode? errorCode);

    [LoggerMessage(EventId = 1102, Level = LogLevel.Information,
        Message = "freebox_connection_status succeeded (state={State}, media={Media}).")]
    private static partial void LogConnectionStatusSucceeded(ILogger logger, string? state, string? media);

    [LoggerMessage(EventId = 1110, Level = LogLevel.Information,
        Message = "Executing freebox_lan_overview (includeHosts={IncludeHosts}, interfaceName={InterfaceName}).")]
    private static partial void LogLanOverviewStarted(ILogger logger, bool includeHosts, string? interfaceName);

    [LoggerMessage(EventId = 1111, Level = LogLevel.Warning, Message = "freebox_lan_overview failed: {ErrorCode}")]
    private static partial void LogLanOverviewFailed(ILogger logger, FreeboxCoreErrorCode? errorCode);

    [LoggerMessage(EventId = 1112, Level = LogLevel.Information,
        Message = "freebox_lan_overview succeeded (interfaces={InterfaceCount}, includeHosts={IncludeHosts}).")]
    private static partial void LogLanOverviewSucceeded(ILogger logger, int interfaceCount, bool includeHosts);

    [LoggerMessage(EventId = 1120, Level = LogLevel.Information, Message = "Executing freebox_wifi_status.")]
    private static partial void LogWifiStatusStarted(ILogger logger);

    [LoggerMessage(EventId = 1121, Level = LogLevel.Warning, Message = "freebox_wifi_status failed: {ErrorCode}")]
    private static partial void LogWifiStatusFailed(ILogger logger, FreeboxCoreErrorCode? errorCode);

    [LoggerMessage(EventId = 1122, Level = LogLevel.Information,
        Message =
            "freebox_wifi_status succeeded (enabled={Enabled}, aps={AccessPointCount}, networks={NetworkCount}).")]
    private static partial void LogWifiStatusSucceeded(
        ILogger logger,
        bool? enabled,
        int accessPointCount,
        int networkCount);
}
