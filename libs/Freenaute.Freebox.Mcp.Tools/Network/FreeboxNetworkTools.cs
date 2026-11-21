namespace Freenaute.Freebox.Mcp.Tools.Network;

[McpServerToolType]
public sealed class FreeboxNetworkTools(IFreeboxOrchestrator orchestrator)
{
    private const int MaxInterfaces = 32;
    private const int MaxHostsPerInterface = 128;
    private const int MaxAccessPoints = 32;
    private const int MaxBss = 128;

    [McpServerTool(Name = "freebox_connection_status", Title = "Get Freebox connection status", ReadOnly = true,
        Idempotent = true, Destructive = false, OpenWorld = true, UseStructuredContent = true,
        OutputSchemaType = typeof(ConnectionToolResult))]
    [Description("Returns bounded connection state and traffic counters; network addresses are excluded.")]
    public async Task<ConnectionToolResult> GetConnectionAsync(CancellationToken cancellationToken)
    {
        FreeboxCoreResult<FreeboxNetworkSnapshot> result = await orchestrator
            .GetNetworkSnapshotAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return ToolResultFactory.Failure<ConnectionToolResult>(result.Error!);
        }

        FreeboxConnectionSnapshot value = result.Value!.Connection;
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
        if (interfaceName is not null && (interfaceName.Length is < 1 or > 64 || interfaceName.Any(char.IsControl)))
        {
            throw new ArgumentException("interfaceName must be 1 to 64 characters and contain no control characters.",
                nameof(interfaceName));
        }

        FreeboxCoreResult<FreeboxNetworkSnapshot> result = await orchestrator
            .GetNetworkSnapshotAsync(includeHosts, interfaceName, cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return ToolResultFactory.Failure<LanToolResult>(result.Error!);
        }

        FreeboxLanSnapshot value = result.Value!.Lan;
        LanInterfaceToolResult[] summaries = value.Interfaces
            .Where(item => interfaceName is null || string.Equals(item.Name, interfaceName, StringComparison.Ordinal))
            .Take(MaxInterfaces)
            .Select(item =>
            {
                string name = item.Name is null ? string.Empty : Safe.Text(item.Name, 64);
                LanHostToolResult[]? hosts = includeHosts && value.Hosts.TryGetValue(
                    item.Name ?? string.Empty, out FreeboxLanHostSnapshot[]? raw)
                    ? raw.Take(MaxHostsPerInterface).Select(MapHost).ToArray()
                    : null;
                return new LanInterfaceToolResult(name, item.HostCount, hosts);
            }).ToArray();

        return new LanToolResult(Safe.Text(value.Name, 64), Safe.Text(value.Type, 32), Safe.Text(value.Mode, 32),
            summaries);
    }

    [McpServerTool(Name = "freebox_wifi_status", Title = "Get Freebox Wi-Fi status", ReadOnly = true, Idempotent = true,
        Destructive = false, OpenWorld = true, UseStructuredContent = true, OutputSchemaType = typeof(WifiToolResult))]
    [Description("Returns bounded sanitized Wi-Fi state. Host identifiers, BSSIDs, names, and SSIDs are excluded.")]
    public async Task<WifiToolResult> GetWifiAsync(CancellationToken cancellationToken)
    {
        FreeboxCoreResult<FreeboxNetworkSnapshot> result = await orchestrator
            .GetNetworkSnapshotAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return ToolResultFactory.Failure<WifiToolResult>(result.Error!);
        }

        FreeboxWifiSnapshot value = result.Value!.Wifi;
        return new WifiToolResult(value.Enabled, Safe.Text(value.MacFilterState, 32), Safe.Text(value.State, 32),
            value.AccessPoints.Take(MaxAccessPoints).Select(item =>
                new WifiAccessPointToolResult(item.Id, string.Empty, Safe.Text(item.State, 32))).ToArray(),
            value.Networks.Take(MaxBss).Select(MapBss).ToArray());
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
}
