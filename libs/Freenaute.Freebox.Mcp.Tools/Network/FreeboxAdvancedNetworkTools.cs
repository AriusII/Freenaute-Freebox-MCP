using System.Net;
using Freenaute.Freebox.Client.Domains.Network;
using Freenaute.Freebox.Mapper.Contracts.Network.Connection;
using Freenaute.Freebox.Mapper.Contracts.Network.Dhcp;
using Freenaute.Freebox.Mapper.Contracts.Network.PortForwarding;
using Microsoft.Extensions.Logging.Abstractions;

namespace Freenaute.Freebox.Mcp.Tools.Network;

[McpServerToolType]
public sealed partial class FreeboxAdvancedNetworkTools(
    IFreeboxClient client,
    ILogger<FreeboxAdvancedNetworkTools>? logger = null)
{
    private const int MaxLeases = 128;
    private const int MaxPortForwards = 128;
    private const int MaxCommentLength = 256;

    private readonly ILogger<FreeboxAdvancedNetworkTools> _logger =
        logger ?? NullLogger<FreeboxAdvancedNetworkTools>.Instance;

    [McpServerTool(Name = "freebox_network_dhcp_leases", Title = "List Freebox DHCP leases", ReadOnly = true,
        Idempotent = true, Destructive = false, OpenWorld = true, UseStructuredContent = true,
        OutputSchemaType = typeof(DhcpLeasesToolResult))]
    [Description(
        "Returns bounded DHCP lease metadata. MAC addresses, IP addresses, hostnames, and lease identities are excluded.")]
    public async Task<DhcpLeasesToolResult> GetDhcpLeasesAsync(
        [Description("Maximum number of leases to return (1 to 128). Defaults to 64.")]
        int limit = 64,
        CancellationToken cancellationToken = default)
    {
        const string toolName = "freebox_network_dhcp_leases";
        LogToolStarted(_logger, toolName);
        ValidateLimit(limit, MaxLeases);

        try
        {
            IFreeboxNetworkApi network = client.Network;
            DhcpDynamicLease[] dynamicLeases =
                await network.Dhcp.GetDynamicLeasesAsync(cancellationToken).ConfigureAwait(false);
            DhcpStaticLease[] staticLeases =
                await network.Dhcp.GetStaticLeasesAsync(cancellationToken).ConfigureAwait(false);
            DhcpLeaseToolResult[] leases =
            [
                .. staticLeases.Select(lease => MapLease(lease, "static"))
                    .Concat(dynamicLeases.Select(lease => MapLease(lease, "dynamic")))
                    .Take(limit)
            ];

            LogToolSucceeded(_logger, toolName, leases.Length);
            return new DhcpLeasesToolResult(staticLeases.Length + dynamicLeases.Length, leases);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            LogToolFailed(_logger, toolName, exception.Message);
            return ToolResultFactory.Failure<DhcpLeasesToolResult>(FreeboxErrorMapper.Map(exception));
        }
    }

    [McpServerTool(Name = "freebox_network_port_forwards", Title = "List Freebox port forwards", ReadOnly = true,
        Idempotent = true, Destructive = false, OpenWorld = true, UseStructuredContent = true,
        OutputSchemaType = typeof(PortForwardsToolResult))]
    [Description(
        "Returns bounded port-forward metadata. LAN/source addresses, hostnames, comments, and raw IDs are excluded.")]
    public async Task<PortForwardsToolResult> GetPortForwardsAsync(
        [Description("Maximum number of port forwards to return (1 to 128). Defaults to 64.")]
        int limit = 64,
        CancellationToken cancellationToken = default)
    {
        const string toolName = "freebox_network_port_forwards";
        LogToolStarted(_logger, toolName);
        ValidateLimit(limit, MaxPortForwards);

        try
        {
            PortForwardingConfig[] forwards =
                await client.Network.Nat.GetForwardingsAsync(cancellationToken).ConfigureAwait(false);
            PortForwardToolResult[] mapped = [.. forwards.Take(limit).Select(MapPortForward)];
            LogToolSucceeded(_logger, toolName, mapped.Length);
            return new PortForwardsToolResult(forwards.Length, mapped);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            LogToolFailed(_logger, toolName, exception.Message);
            return ToolResultFactory.Failure<PortForwardsToolResult>(FreeboxErrorMapper.Map(exception));
        }
    }

    [McpServerTool(Name = "freebox_network_ipv6_status", Title = "Get Freebox IPv6 status", ReadOnly = true,
        Idempotent = true, Destructive = false, OpenWorld = true, UseStructuredContent = true,
        OutputSchemaType = typeof(Ipv6StatusToolResult))]
    [Description(
        "Returns sanitized IPv6 status. Link-local addresses and delegated prefixes are excluded.")]
    public async Task<Ipv6StatusToolResult> GetIpv6StatusAsync(CancellationToken cancellationToken)
    {
        const string toolName = "freebox_network_ipv6_status";
        LogToolStarted(_logger, toolName);
        try
        {
            ConnectionIpv6Configuration configuration =
                await client.Network.Connection.GetIpv6ConfigurationAsync(cancellationToken).ConfigureAwait(false);
            LogToolSucceeded(_logger, toolName, configuration.Delegations?.Length ?? 0);
            return new Ipv6StatusToolResult(
                configuration.Ipv6Enabled,
                configuration.Ipv6Firewall,
                configuration.Ipv6PrefixFirewall,
                !string.IsNullOrWhiteSpace(configuration.Ipv6Ll),
                Math.Min(configuration.Delegations?.Length ?? 0, MaxLeases));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            LogToolFailed(_logger, toolName, exception.Message);
            return ToolResultFactory.Failure<Ipv6StatusToolResult>(FreeboxErrorMapper.Map(exception));
        }
    }

    [McpServerTool(Name = "freebox_network_port_forward_create", Title = "Create Freebox port forward",
        ReadOnly = false, Idempotent = false, Destructive = false, OpenWorld = true, UseStructuredContent = true,
        OutputSchemaType = typeof(PortForwardMutationToolResult))]
    [Description("Creates a port forward after validating protocol, IPv4 addresses, ports, and comment length.")]
    public async Task<PortForwardMutationToolResult> CreatePortForwardAsync(
        [Description("IP protocol: tcp or udp.")] string protocol,
        [Description("First external port (1 to 65535).")] int wanPortStart,
        [Description("Private IPv4 destination address.")] string lanIp,
        [Description("Destination port (1 to 65535).")] int lanPort,
        [Description("Last external port. Must be at least the first external port.")] int? wanPortEnd = null,
        [Description("Optional allowed source IPv4 address.")] string? sourceIp = null,
        [Description("Optional bounded comment (at most 256 characters).")] string? comment = null,
        [Description("Whether the forwarding rule is enabled.")] bool enabled = true,
        CancellationToken cancellationToken = default)
    {
        const string toolName = "freebox_network_port_forward_create";
        LogToolStarted(_logger, toolName);
        string normalizedProtocol = ValidateProtocol(protocol);
        ValidatePort(wanPortStart, nameof(wanPortStart));
        ValidatePort(lanPort, nameof(lanPort));
        if (wanPortEnd is not null)
        {
            ValidatePort(wanPortEnd.Value, nameof(wanPortEnd));
            if (wanPortEnd.Value < wanPortStart)
            {
                ThrowInvalidRequest("wanPortEnd must be greater than or equal to wanPortStart.");
            }
        }

        ValidateIpv4(lanIp, nameof(lanIp));
        ValidateOptionalIpv4(sourceIp, nameof(sourceIp));
        ValidateComment(comment);

        try
        {
            var request = new CreatePortForwardingRequest
            {
                Enabled = enabled,
                IpProto = normalizedProtocol,
                WanPortStart = Optional<StringOrInteger>.FromValue(StringOrInteger.FromInteger(wanPortStart)),
                LanIp = lanIp,
                LanPort = lanPort,
                WanPortEnd = wanPortEnd is null
                    ? Optional<long>.Unset
                    : Optional<long>.FromValue(wanPortEnd.Value),
                SrcIp = sourceIp is null ? Optional<string>.Unset : Optional<string>.FromValue(sourceIp),
                Comment = comment is null ? Optional<string>.Unset : Optional<string>.FromValue(comment)
            };
            PortForwardingConfig created =
                await client.Network.Nat.CreateForwardingAsync(request, cancellationToken).ConfigureAwait(false);
            PortForwardToolResult mapped = MapPortForward(created);
            LogMutationSucceeded(_logger, toolName, mapped.StableId);
            return new PortForwardMutationToolResult(mapped.StableId, true, mapped);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            LogToolFailed(_logger, toolName, exception.Message);
            return ToolResultFactory.Failure<PortForwardMutationToolResult>(FreeboxErrorMapper.Map(exception));
        }
    }

    [McpServerTool(Name = "freebox_network_port_forward_update", Title = "Update Freebox port forward",
        ReadOnly = false, Idempotent = true, Destructive = false, OpenWorld = true, UseStructuredContent = true,
        OutputSchemaType = typeof(PortForwardMutationToolResult))]
    [Description("Updates explicitly supplied port-forward fields after validating every supplied value.")]
    public async Task<PortForwardMutationToolResult> UpdatePortForwardAsync(
        [Description("Numeric port-forward ID.")] long id,
        [Description("Replacement enabled state.")] bool? enabled = null,
        [Description("Replacement protocol: tcp or udp.")] string? protocol = null,
        [Description("Replacement first external port.")] int? wanPortStart = null,
        [Description("Replacement last external port.")] int? wanPortEnd = null,
        [Description("Replacement private IPv4 destination address.")] string? lanIp = null,
        [Description("Replacement destination port.")] int? lanPort = null,
        [Description("Replacement allowed source IPv4 address.")] string? sourceIp = null,
        [Description("Replacement comment (at most 256 characters).")] string? comment = null,
        CancellationToken cancellationToken = default)
    {
        const string toolName = "freebox_network_port_forward_update";
        LogToolStarted(_logger, toolName);
        ValidateId(id);
        if (enabled is null && protocol is null && wanPortStart is null && wanPortEnd is null &&
            lanIp is null && lanPort is null && sourceIp is null && comment is null)
        {
            ThrowInvalidRequest("At least one field must be supplied.");
        }

        string? normalizedProtocol = protocol is null ? null : ValidateProtocol(protocol);
        if (wanPortStart is not null)
        {
            ValidatePort(wanPortStart.Value, nameof(wanPortStart));
        }

        if (wanPortEnd is not null)
        {
            ValidatePort(wanPortEnd.Value, nameof(wanPortEnd));
        }

        if (wanPortStart is not null && wanPortEnd is not null && wanPortEnd.Value < wanPortStart.Value)
        {
            ThrowInvalidRequest("wanPortEnd must be greater than or equal to wanPortStart.");
        }

        if (lanPort is not null)
        {
            ValidatePort(lanPort.Value, nameof(lanPort));
        }

        ValidateOptionalIpv4(lanIp, nameof(lanIp));
        ValidateOptionalIpv4(sourceIp, nameof(sourceIp));
        ValidateComment(comment);

        try
        {
            var patch = new PortForwardingPatch
            {
                Enabled = enabled is null ? Optional<bool>.Unset : Optional<bool>.FromValue(enabled.Value),
                IpProto = normalizedProtocol is null
                    ? Optional<string>.Unset
                    : Optional<string>.FromValue(normalizedProtocol),
                WanPortStart = wanPortStart is null
                    ? Optional<StringOrInteger>.Unset
                    : Optional<StringOrInteger>.FromValue(StringOrInteger.FromInteger(wanPortStart.Value)),
                WanPortEnd = wanPortEnd is null
                    ? Optional<long>.Unset
                    : Optional<long>.FromValue(wanPortEnd.Value),
                LanIp = lanIp is null ? Optional<string>.Unset : Optional<string>.FromValue(lanIp),
                LanPort = lanPort is null ? Optional<long>.Unset : Optional<long>.FromValue(lanPort.Value),
                SrcIp = sourceIp is null ? Optional<string>.Unset : Optional<string>.FromValue(sourceIp),
                Comment = comment is null ? Optional<string>.Unset : Optional<string>.FromValue(comment)
            };

            PortForwardingConfig updated =
                await client.Network.Nat.Forwarding(id).UpdateAsync(patch, cancellationToken).ConfigureAwait(false);
            PortForwardToolResult mapped = MapPortForward(updated);
            LogMutationSucceeded(_logger, toolName, mapped.StableId);
            return new PortForwardMutationToolResult(mapped.StableId, true, mapped);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            LogToolFailed(_logger, toolName, exception.Message);
            return ToolResultFactory.Failure<PortForwardMutationToolResult>(FreeboxErrorMapper.Map(exception));
        }
    }

    [McpServerTool(Name = "freebox_network_port_forward_delete", Title = "Delete Freebox port forward",
        ReadOnly = false, Idempotent = true, Destructive = true, OpenWorld = true, UseStructuredContent = true,
        OutputSchemaType = typeof(PortForwardMutationToolResult))]
    [Description("Deletes one port-forward rule. Explicit confirmation is mandatory.")]
    public async Task<PortForwardMutationToolResult> DeletePortForwardAsync(
        [Description("Numeric port-forward ID.")] long id,
        [Description("Must be true to confirm deletion.")] bool confirm,
        CancellationToken cancellationToken = default)
    {
        const string toolName = "freebox_network_port_forward_delete";
        LogToolStarted(_logger, toolName);
        ValidateId(id);
        if (!confirm)
        {
            ThrowInvalidRequest("confirm must be true to delete a port forward.");
        }

        try
        {
            await client.Network.Nat.Forwarding(id).DeleteAsync(cancellationToken).ConfigureAwait(false);
            string stableId = Safe.StableIdentifier(id.ToString(CultureInfo.InvariantCulture));
            LogMutationSucceeded(_logger, toolName, stableId);
            return new PortForwardMutationToolResult(stableId, true, null);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            LogToolFailed(_logger, toolName, exception.Message);
            return ToolResultFactory.Failure<PortForwardMutationToolResult>(FreeboxErrorMapper.Map(exception));
        }
    }

    private static DhcpLeaseToolResult MapLease(DhcpStaticLease lease, string kind)
    {
        return new DhcpLeaseToolResult(
            Safe.StableIdentifier(lease.Id ?? lease.Mac ?? lease.Ip),
            kind,
            true,
            !string.IsNullOrWhiteSpace(lease.Mac),
            !string.IsNullOrWhiteSpace(lease.Ip),
            HasHostname(lease.Hostname, lease.Host?.PrimaryName),
            null,
            null,
            null,
            lease.Options?.Length ?? 0);
    }

    private static DhcpLeaseToolResult MapLease(DhcpDynamicLease lease, string kind)
    {
        return new DhcpLeaseToolResult(
            Safe.StableIdentifier(lease.Mac ?? lease.Ip ?? lease.Hostname),
            kind,
            lease.IsStatic ?? false,
            !string.IsNullOrWhiteSpace(lease.Mac),
            !string.IsNullOrWhiteSpace(lease.Ip),
            HasHostname(lease.Hostname, lease.Host?.PrimaryName),
            lease.LeaseRemaining,
            lease.AssignTime,
            lease.RefreshTime,
            lease.Options?.Length ?? 0);
    }

    private static PortForwardToolResult MapPortForward(PortForwardingConfig forwarding)
    {
        return new PortForwardToolResult(
            Safe.StableIdentifier(forwarding.Id?.ToString(CultureInfo.InvariantCulture)),
            forwarding.Enabled,
            Safe.Text(forwarding.IpProto, 8),
            MapPortStart(forwarding.WanPortStart),
            forwarding.WanPortEnd,
            forwarding.LanPort,
            !string.IsNullOrWhiteSpace(forwarding.LanIp),
            !string.IsNullOrWhiteSpace(forwarding.SrcIp),
            !string.IsNullOrWhiteSpace(forwarding.Comment),
            HasHostname(forwarding.Hostname, forwarding.Host?.PrimaryName)
        );
    }

    private static bool HasHostname(string? directName, string? hostName)
    {
        return !string.IsNullOrWhiteSpace(directName) || !string.IsNullOrWhiteSpace(hostName);
    }

    private static string MapPortStart(StringOrInteger? value)
    {
        if (value is not { IsInitialized: true } port)
        {
            return string.Empty;
        }

        return port.Kind == StringOrIntegerKind.Integer
            ? port.Integer.ToString(CultureInfo.InvariantCulture)
            : Safe.Text(port.String, 32);
    }

    private static string ValidateProtocol(string? protocol)
    {
        string normalized = protocol?.Trim().ToLowerInvariant() ?? string.Empty;
        if (normalized is not ("tcp" or "udp"))
        {
            ThrowInvalidRequest("protocol must be tcp or udp.");
        }

        return normalized;
    }

    private static void ValidatePort(int port, string parameter)
    {
        if (port is < 1 or > 65535)
        {
            ThrowInvalidRequest($"{parameter} must be between 1 and 65535.");
        }
    }

    private static void ValidateId(long id)
    {
        if (id <= 0)
        {
            ThrowInvalidRequest("id must be > 0.");
        }
    }

    private static void ValidateIpv4(string value, string parameter)
    {
        if (string.IsNullOrWhiteSpace(value) || !IPAddress.TryParse(value, out IPAddress? address) ||
            address.AddressFamily != global::System.Net.Sockets.AddressFamily.InterNetwork)
        {
            ThrowInvalidRequest($"{parameter} must be a valid IPv4 address.");
        }
    }

    private static void ValidateOptionalIpv4(string? value, string parameter)
    {
        if (value is not null)
        {
            ValidateIpv4(value, parameter);
        }
    }

    private static void ValidateComment(string? comment)
    {
        if (comment is not null &&
            (comment.Length > MaxCommentLength || comment.Any(char.IsControl)))
        {
            ThrowInvalidRequest($"comment must be at most {MaxCommentLength} characters and contain no control characters.");
        }
    }

    private static void ValidateLimit(int limit, int maximum)
    {
        if (limit is < 1 || limit > maximum)
        {
            ThrowInvalidRequest($"limit must be between 1 and {maximum}.");
        }
    }

    private static void ThrowInvalidRequest(string message)
    {
        throw new InvalidOperationException($"{FreeboxCoreErrorCode.InvalidRequest}: {message}");
    }

    [LoggerMessage(EventId = 1200, Level = LogLevel.Information, Message = "Executing {ToolName}.")]
    private static partial void LogToolStarted(ILogger logger, string toolName);

    [LoggerMessage(EventId = 1201, Level = LogLevel.Information, Message = "{ToolName} succeeded (count={Count}).")]
    private static partial void LogToolSucceeded(ILogger logger, string toolName, int count);

    [LoggerMessage(EventId = 1202, Level = LogLevel.Warning, Message = "{ToolName} failed: {Reason}")]
    private static partial void LogToolFailed(ILogger logger, string toolName, string? reason);

    [LoggerMessage(EventId = 1203, Level = LogLevel.Information,
        Message = "{ToolName} mutation succeeded (stableId={StableId}).")]
    private static partial void LogMutationSucceeded(ILogger logger, string toolName, string stableId);
}
