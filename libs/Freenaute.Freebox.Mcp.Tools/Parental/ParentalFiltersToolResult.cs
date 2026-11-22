namespace Freenaute.Freebox.Mcp.Tools.Parental;

public sealed record ParentalFiltersToolResult(
    int TotalCount,
    ParentalFilterToolResult[] Filters);
