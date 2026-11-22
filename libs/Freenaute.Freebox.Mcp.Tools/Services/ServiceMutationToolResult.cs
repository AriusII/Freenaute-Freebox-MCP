namespace Freenaute.Freebox.Mcp.Tools.Services;

public sealed record ServiceMutationToolResult(
    bool Changed,
    string Service,
    string Message);
