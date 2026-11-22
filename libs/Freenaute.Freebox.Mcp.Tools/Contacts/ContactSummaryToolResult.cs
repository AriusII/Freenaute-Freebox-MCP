namespace Freenaute.Freebox.Mcp.Tools.Contacts;

public sealed record ContactSummaryToolResult(
    string Id,
    string DisplayName,
    string? Company,
    long? LastUpdate,
    int NumberCount,
    int EmailCount,
    int AddressCount,
    int UrlCount);
