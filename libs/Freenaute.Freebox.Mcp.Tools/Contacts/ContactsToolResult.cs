namespace Freenaute.Freebox.Mcp.Tools.Contacts;

public sealed record ContactsToolResult(
    string? Query,
    int TotalMatches,
    ContactSummaryToolResult[] Contacts);
