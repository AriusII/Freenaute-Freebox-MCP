using Microsoft.Extensions.Logging.Abstractions;

namespace Freenaute.Freebox.Mcp.Resources.Live;

public sealed partial class OrchestratorFreeboxLiveResourceSource(
    IFreeboxOrchestrator orchestrator,
    ILogger<OrchestratorFreeboxLiveResourceSource>? logger = null)
    : IFreeboxLiveResourceSource
{
    private const int MaxComponents = 16;
    private const int MaxValueLength = 32;

    private readonly ILogger<OrchestratorFreeboxLiveResourceSource> _logger =
        logger ?? NullLogger<OrchestratorFreeboxLiveResourceSource>.Instance;

    public async ValueTask<FreeboxLiveSnapshot> GetSnapshotAsync(CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(orchestrator);
        LogSnapshotStarted(_logger);

        FreeboxCoreResult<FreeboxConnectionResult> connectionTask =
            await orchestrator.GetConnectionStatusAsync(cancellationToken).ConfigureAwait(false);
        FreeboxCoreResult<FreeboxNetworkSnapshot> networkTask =
            await orchestrator.GetNetworkSnapshotAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        FreeboxCoreResult<FreeboxSystemSnapshot> systemTask =
            await orchestrator.GetSystemSnapshotAsync(cancellationToken).ConfigureAwait(false);

        var text = new StringBuilder("# Freebox status\n\n");
        AppendAvailability(text, connectionTask);
        AppendNetwork(text, networkTask);
        AppendSystem(text, systemTask);
        LogSnapshotCompleted(
            _logger,
            connectionTask.IsSuccess,
            networkTask.IsSuccess,
            systemTask.IsSuccess);
        return new FreeboxLiveSnapshot(BoundAndSanitize(text.ToString()), "text/markdown");
    }

    private static string BoundAndSanitize(string content)
    {
        // Defense in depth: the payload above is allow-listed, but redact secret-looking
        // assignments anyway and keep the snapshot bounded even if the allow-list grows.
        string sanitized = FreeboxSecretRedactor.Redact(content);
        const int MaxSnapshotCharacters = 32 * 1024;
        return sanitized.Length <= MaxSnapshotCharacters
            ? sanitized
            : sanitized[..MaxSnapshotCharacters] + "\n\n[status truncated]\n";
    }

    private static void AppendAvailability(
        StringBuilder text,
        FreeboxCoreResult<FreeboxConnectionResult> result)
    {
        text.Append("## Pairing\n\n");
        if (!result.IsSuccess || result.Value is null)
        {
            // Only the error code is reported; the message may contain tokens, URLs or host names.
            text.Append("- availability: unavailable (error: ")
                .Append(FormatToken(result.Error?.Code.ToString().ToLowerInvariant()))
                .Append(")\n");
            return;
        }

        text.Append("- credential store available: ")
            .Append(FormatBoolean(result.Value.CredentialStoreAvailable))
            .Append("\n- credential state: ")
            .Append(FormatToken(result.Value.CredentialState.ToString().ToLowerInvariant()))
            .Append('\n');
    }

    private static void AppendNetwork(StringBuilder text, FreeboxCoreResult<FreeboxNetworkSnapshot> result)
    {
        text.Append("\n## Network\n\n");
        if (!result.IsSuccess || result.Value is null)
        {
            // Only the error code is reported; the message may contain tokens, URLs or host names.
            text.Append("- availability: unavailable (error: ")
                .Append(FormatToken(result.Error?.Code.ToString().ToLowerInvariant()))
                .Append(")\n");
            return;
        }

        FreeboxNetworkSnapshot snapshot = result.Value;
        text.Append("- connection state: ")
            .Append(FormatToken(snapshot.Connection.State))
            .Append("\n- connection type: ")
            .Append(FormatToken(snapshot.Connection.Type))
            .Append("\n- connection media: ")
            .Append(FormatToken(snapshot.Connection.Media))
            .Append("\n- upload rate (bytes/s): ")
            .Append(FormatNumber(snapshot.Connection.RateUp))
            .Append("\n- download rate (bytes/s): ")
            .Append(FormatNumber(snapshot.Connection.RateDown))
            .Append("\n- uploaded bytes: ")
            .Append(FormatNumber(snapshot.Connection.BytesUp))
            .Append("\n- downloaded bytes: ")
            .Append(FormatNumber(snapshot.Connection.BytesDown))
            .Append("\n- Wi-Fi enabled: ")
            .Append(FormatBoolean(snapshot.Wifi.Enabled))
            .Append("\n- Wi-Fi state: ")
            .Append(FormatToken(snapshot.Wifi.State))
            .Append("\n- access point count: ")
            .Append(Math.Min(snapshot.Wifi.AccessPoints.Length, MaxComponents))
            .Append("\n- network count: ")
            .Append(Math.Min(snapshot.Wifi.Networks.Length, MaxComponents))
            .Append('\n');
    }

    private static void AppendSystem(StringBuilder text, FreeboxCoreResult<FreeboxSystemSnapshot> result)
    {
        text.Append("\n## System\n\n");
        if (!result.IsSuccess || result.Value is null)
        {
            // Only the error code is reported; the message may contain tokens, URLs or host names.
            text.Append("- availability: unavailable (error: ")
                .Append(FormatToken(result.Error?.Code.ToString().ToLowerInvariant()))
                .Append(")\n");
            return;
        }

        FreeboxSystemSnapshot snapshot = result.Value;
        text.Append("- uptime (seconds): ")
            .Append(FormatNumber(snapshot.UptimeSeconds))
            .Append("\n- authenticated: ")
            .Append(FormatBoolean(snapshot.BoxAuthenticated))
            .Append("\n- disk status: ")
            .Append(FormatToken(snapshot.DiskStatus))
            .Append("\n- sensor count: ")
            .Append(Math.Min(snapshot.Sensors.Length, MaxComponents))
            .Append("\n- fan count: ")
            .Append(Math.Min(snapshot.Fans.Length, MaxComponents))
            .Append("\n- expansion count: ")
            .Append(Math.Min(snapshot.Expansions.Length, MaxComponents))
            .Append('\n');
    }

    private static string FormatToken(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "unknown";
        }

        var sanitized = new StringBuilder(Math.Min(value.Length, MaxValueLength));
        foreach (char character in value)
        {
            if (sanitized.Length == MaxValueLength)
            {
                break;
            }

            if (character is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9' or '_' or '-' or '.')
            {
                sanitized.Append(character);
            }
            else
            {
                sanitized.Append('_');
            }
        }

        return sanitized.ToString();
    }

    private static string FormatBoolean(bool? value)
    {
        return value switch
        {
            true => "true",
            false => "false",
            _ => "unknown"
        };
    }

    private static string FormatNumber(long? value)
    {
        return value?.ToString(CultureInfo.InvariantCulture) ?? "unknown";
    }

    [LoggerMessage(EventId = 3010, Level = LogLevel.Information, Message = "Building live Freebox status snapshot.")]
    private static partial void LogSnapshotStarted(ILogger logger);

    [LoggerMessage(EventId = 3011, Level = LogLevel.Information,
        Message = "Live snapshot built (connectionOk={ConnectionOk}, networkOk={NetworkOk}, systemOk={SystemOk}).")]
    private static partial void LogSnapshotCompleted(
        ILogger logger,
        bool connectionOk,
        bool networkOk,
        bool systemOk);
}
