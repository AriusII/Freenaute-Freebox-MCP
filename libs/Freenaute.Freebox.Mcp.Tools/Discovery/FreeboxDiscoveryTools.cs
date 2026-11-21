namespace Freenaute.Freebox.Mcp.Tools.Discovery;

[McpServerToolType]
public sealed class FreeboxDiscoveryTools(IFreeboxOrchestrator orchestrator)
{
    [McpServerTool(Name = "freebox_discover", Title = "Discover Freebox", ReadOnly = true, Idempotent = true,
        Destructive = false, OpenWorld = true, UseStructuredContent = true,
        OutputSchemaType = typeof(DiscoveryToolResult))]
    [Description("Discovers the reachable Freebox and returns bounded, non-identifying API capability metadata.")]
    public async Task<DiscoveryToolResult> DiscoverAsync(CancellationToken cancellationToken)
    {
        FreeboxCoreResult<FreeboxDiscoverySnapshot> result = await orchestrator.DiscoverAsync(cancellationToken)
            .ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return ToolResultFactory.Failure<DiscoveryToolResult>(result.Error!);
        }

        FreeboxDiscoverySnapshot value = result.Value!;
        return new DiscoveryToolResult(
            Safe.Text(value.BoxModel, 64),
            Safe.Text(value.ApiVersion, 32),
            value.HttpsAvailable,
            value.HttpsPort);
    }
}
