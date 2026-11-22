using Microsoft.Extensions.Logging.Abstractions;

namespace Freenaute.Freebox.Mcp.Tools.AirMedia;

[McpServerToolType]
public sealed partial class FreeboxAirMediaTools(
    IFreeboxAirMediaApi airMedia,
    ILogger<FreeboxAirMediaTools>? logger = null)
{
    private const int MaxReceivers = 32;
    private readonly IFreeboxAirMediaApi _airMedia = airMedia ?? throw new ArgumentNullException(nameof(airMedia));

    private readonly ILogger<FreeboxAirMediaTools> _logger =
        logger ?? NullLogger<FreeboxAirMediaTools>.Instance;

    public FreeboxAirMediaTools(IFreeboxClient client, ILogger<FreeboxAirMediaTools>? logger = null)
        : this(client?.AirMedia ?? throw new ArgumentNullException(nameof(client)), logger)
    {
    }

    [McpServerTool(Name = "freebox_airmedia_status", Title = "Get Freebox AirMedia status", ReadOnly = true,
        Idempotent = true, Destructive = false, OpenWorld = true, UseStructuredContent = true,
        OutputSchemaType = typeof(AirMediaStatusToolResult))]
    [Description("Returns bounded sanitized AirMedia state and receiver capabilities.")]
    public async Task<AirMediaStatusToolResult> GetStatusAsync(CancellationToken cancellationToken)
    {
        LogToolStarted(_logger, "freebox_airmedia_status");
        try
        {
            AirMediaConfigResponse configuration =
                await _airMedia.GetConfigurationAsync(cancellationToken).ConfigureAwait(false);
            IReadOnlyList<AirMediaReceiverResponse> receivers =
                await _airMedia.GetReceiversAsync(cancellationToken).ConfigureAwait(false);

            LogToolSucceeded(_logger, "freebox_airmedia_status", receivers.Count);
            return new AirMediaStatusToolResult(
                configuration.Enabled,
                [.. receivers.Take(MaxReceivers).Select(MapReceiver)]);
        }
        catch (Exception exception)
        {
            LogToolFailed(_logger, "freebox_airmedia_status", exception.Message);
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
        LogToolStarted(_logger, "freebox_airmedia_receivers");
        try
        {
            IReadOnlyList<AirMediaReceiverResponse> receivers =
                await _airMedia.GetReceiversAsync(cancellationToken).ConfigureAwait(false);
            LogToolSucceeded(_logger, "freebox_airmedia_receivers", receivers.Count);
            return new AirMediaReceiversToolResult(
                [.. receivers.Take(MaxReceivers).Select(MapReceiver)]);
        }
        catch (Exception exception)
        {
            LogToolFailed(_logger, "freebox_airmedia_receivers", exception.Message);
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

    [LoggerMessage(EventId = 1700, Level = LogLevel.Information, Message = "Executing {ToolName}.")]
    private static partial void LogToolStarted(ILogger logger, string toolName);

    [LoggerMessage(EventId = 1701, Level = LogLevel.Information,
        Message = "{ToolName} succeeded (receivers={ReceiverCount}).")]
    private static partial void LogToolSucceeded(ILogger logger, string toolName, int receiverCount);

    [LoggerMessage(EventId = 1702, Level = LogLevel.Warning, Message = "{ToolName} failed: {Reason}")]
    private static partial void LogToolFailed(ILogger logger, string toolName, string? reason);
}
