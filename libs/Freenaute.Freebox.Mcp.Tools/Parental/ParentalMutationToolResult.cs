namespace Freenaute.Freebox.Mcp.Tools.Parental;

public sealed record ParentalMutationToolResult(
    bool Success,
    string Status,
    string StableId = "");
