namespace Freenaute.Freebox.Mcp.Tools.Pairing;

[McpServerToolType]
public sealed class FreeboxPairingTools(IFreeboxOrchestrator orchestrator)
{
    [McpServerTool(Name = "freebox_pairing_status", Title = "Get Freebox pairing status", ReadOnly = true,
        Idempotent = true, Destructive = false, OpenWorld = true, UseStructuredContent = true,
        OutputSchemaType = typeof(PairingStatusToolResult))]
    [Description(
        "Reports durable credential state without exposing credentials. It does not initiate or poll pairing.")]
    public async Task<PairingStatusToolResult> GetStatusAsync(CancellationToken cancellationToken)
    {
        FreeboxCoreResult<FreeboxConnectionResult> result =
            await orchestrator.GetConnectionStatusAsync(cancellationToken).ConfigureAwait(false);
        return result.IsSuccess
            ? new PairingStatusToolResult(
                result.Value!.CredentialStoreAvailable,
                result.Value.CredentialState.ToString())
            : ToolResultFactory.Failure<PairingStatusToolResult>(result.Error!);
    }

    [McpServerTool(Name = "freebox_pairing_initiate", Title = "Initiate Freebox pairing", ReadOnly = false,
        Idempotent = false, Destructive = false, OpenWorld = true, UseStructuredContent = true,
        OutputSchemaType = typeof(PairingInitiationToolResult))]
    [Description("Starts pairing only when explicitly enabled by host policy. Physical approval is still required.")]
    public async Task<PairingInitiationToolResult> InitiateAsync(CancellationToken cancellationToken)
    {
        FreeboxCoreResult<FreeboxPairingResult> result =
            await orchestrator.BeginPairingAsync(cancellationToken).ConfigureAwait(false);
        return result.IsSuccess
            ? new PairingInitiationToolResult(true, result.Value!.State.ToString(),
                result.Value.State == FreeboxCredentialState.Approved
                    ? "The application is already paired."
                    : "Pairing is pending. Physically approve the application on the Freebox.")
            : ToolResultFactory.Failure<PairingInitiationToolResult>(result.Error!);
    }

    [McpServerTool(Name = "freebox_pairing_wait", Title = "Wait for Freebox pairing approval", ReadOnly = false,
        Idempotent = true, Destructive = false, OpenWorld = true, UseStructuredContent = true,
        OutputSchemaType = typeof(PairingApprovalToolResult))]
    [Description(
        "Waits for physical approval of an existing or previously initiated pairing. It can be safely cancelled and resumed. Optional polling and timeout values can be provided.")]
    public async Task<PairingApprovalToolResult> WaitAsync(
        [Description("Optional polling interval in seconds (must be > 0). Defaults to host policy.")]
        int? pollIntervalSeconds = null,
        [Description("Optional timeout in seconds (must be > 0). Defaults to host policy.")]
        int? timeoutSeconds = null,
        CancellationToken cancellationToken = default)
    {
        if (pollIntervalSeconds is <= 0)
        {
            return ToolResultFactory.Failure<PairingApprovalToolResult>(new FreeboxCoreError(
                FreeboxCoreErrorCode.InvalidRequest,
                "pollIntervalSeconds must be > 0."));
        }

        if (timeoutSeconds is <= 0)
        {
            return ToolResultFactory.Failure<PairingApprovalToolResult>(new FreeboxCoreError(
                FreeboxCoreErrorCode.InvalidRequest,
                "timeoutSeconds must be > 0."));
        }

        TimeSpan? pollInterval = pollIntervalSeconds is null
            ? null
            : TimeSpan.FromSeconds(pollIntervalSeconds.Value);
        TimeSpan? timeout = timeoutSeconds is null
            ? null
            : TimeSpan.FromSeconds(timeoutSeconds.Value);

        FreeboxCoreResult<FreeboxCredentialResult> result =
            await orchestrator.WaitForApprovalAsync(pollInterval, timeout, cancellationToken).ConfigureAwait(false);
        return result.IsSuccess
            ? new PairingApprovalToolResult(
                result.Value!.State == FreeboxCredentialState.Approved,
                result.Value.State.ToString(),
                result.Value.State == FreeboxCredentialState.Approved
                    ? "Pairing approved and the credential was stored securely."
                    : $"Pairing ended with state '{result.Value.State}'.")
            : ToolResultFactory.Failure<PairingApprovalToolResult>(result.Error!);
    }
}
