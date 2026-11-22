namespace Freenaute.Freebox.Mcp.Tools.Network;

public sealed record DhcpLeaseToolResult(
    string StableId,
    string Kind,
    bool? IsStatic,
    bool HasMacAddress,
    bool HasIpAddress,
    bool HasHostname,
    long? LeaseRemaining,
    long? AssignedAt,
    long? RefreshedAt,
    int OptionCount);

public sealed record DhcpLeasesToolResult(
    int TotalCount,
    DhcpLeaseToolResult[] Leases);

public sealed record PortForwardToolResult(
    string StableId,
    bool? Enabled,
    string Protocol,
    string WanPortStart,
    long? WanPortEnd,
    long? LanPort,
    bool HasLanAddress,
    bool HasSourceAddress,
    bool HasComment,
    bool HasHostname);

public sealed record PortForwardsToolResult(
    int TotalCount,
    PortForwardToolResult[] PortForwards);

public sealed record Ipv6StatusToolResult(
    bool? Enabled,
    bool? FirewallEnabled,
    bool? PrefixFirewallEnabled,
    bool HasLinkLocalAddress,
    int DelegationCount);

public sealed record PortForwardMutationToolResult(
    string StableId,
    bool Changed,
    PortForwardToolResult? PortForward);
