namespace Freenaute.Freebox.Mcp.Tools.AirMedia;

[McpServerToolType]
public sealed class FreeboxAirMediaTools(IFreeboxAirMediaApi airMedia)
{
    private const int MaxReceivers = 32;
    private readonly IFreeboxAirMediaApi _airMedia = airMedia ?? throw new ArgumentNullException(nameof(airMedia));

    public FreeboxAirMediaTools(IFreeboxClient client)
        : this(client?.AirMedia ?? throw new ArgumentNullException(nameof(client)))
    {
    }

    [McpServerTool(Name = "freebox_airmedia_status", Title = "Get Freebox AirMedia status", ReadOnly = true,
        Idempotent = true, Destructive = false, OpenWorld = true, UseStructuredContent = true,
        OutputSchemaType = typeof(AirMediaStatusToolResult))]
    [Description("Returns bounded sanitized AirMedia state and receiver capabilities.")]
    public async Task<AirMediaStatusToolResult> GetStatusAsync(CancellationToken cancellationToken)
    {
        try
        {
            AirMediaConfigResponse configuration =
                await _airMedia.GetConfigurationAsync(cancellationToken).ConfigureAwait(false);
            IReadOnlyList<AirMediaReceiverResponse> receivers =
                await _airMedia.GetReceiversAsync(cancellationToken).ConfigureAwait(false);

            return new AirMediaStatusToolResult(
                configuration.Enabled,
                [.. receivers.Take(MaxReceivers).Select(MapReceiver)]);
        }
        catch (Exception exception)
        {
            return ToolResultFactory.Failure<AirMediaStatusToolResult>(
                FreeboxErrorMapper.Map(exception));
        }
    }

    [McpServerTool(Name = "freebox_airmedia_receivers", Title = "List Freebox AirMedia receivers",
        ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = true,
        UseStructuredContent = true, OutputSchemaType = typeof(AirMediaReceiversToolResult))]
    [Description("Returns bounded sanitized AirMedia receiver names and capabilities.")]
    public async Task<AirMediaReceiversToolResult> GetReceiversAsync(CancellationToken cancellationToken)
    {
        try
        {
            IReadOnlyList<AirMediaReceiverResponse> receivers =
                await _airMedia.GetReceiversAsync(cancellationToken).ConfigureAwait(false);
            return new AirMediaReceiversToolResult(
                [.. receivers.Take(MaxReceivers).Select(MapReceiver)]);
        }
        catch (Exception exception)
        {
            return ToolResultFactory.Failure<AirMediaReceiversToolResult>(
                FreeboxErrorMapper.Map(exception));
        }
    }

    private static AirMediaReceiverToolResult MapReceiver(AirMediaReceiverResponse receiver)
    {
        AirMediaCapabilities capabilities = receiver.Capabilities;
        return new AirMediaReceiverToolResult(
            Safe.Text(receiver.Name, 64),
            receiver.PasswordProtected,
            capabilities.CanDisplayPhotos,
            capabilities.CanPlayAudio,
            capabilities.CanPlayVideo,
            capabilities.CanDisplayScreen);
    }
}
