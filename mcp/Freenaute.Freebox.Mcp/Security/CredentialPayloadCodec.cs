namespace Freenaute.Freebox.Mcp.Security;

internal static class CredentialPayloadCodec
{
    public static byte[] Serialize(FreeboxAppCredentials credentials)
    {
        ArgumentNullException.ThrowIfNull(credentials);
        var payload = new CredentialPayload(
            FreeboxAppCredentials.FormatVersion,
            credentials.ApplicationId,
            credentials.GetAppToken(),
            credentials.Status,
            credentials.TrackId,
            credentials.CreatedAt,
            credentials.UpdatedAt);
        return JsonSerializer.SerializeToUtf8Bytes(
            payload,
            CredentialPayloadJsonContext.Default.CredentialPayload);
    }

    public static FreeboxAppCredentials? Deserialize(ReadOnlySpan<byte> json)
    {
        try
        {
            CredentialPayload? payload = JsonSerializer.Deserialize(
                json,
                CredentialPayloadJsonContext.Default.CredentialPayload);
            if (payload is null || payload.FormatVersion != 1 ||
                string.IsNullOrWhiteSpace(payload.ApplicationId) ||
                string.IsNullOrWhiteSpace(payload.AppToken))
            {
                throw new InvalidDataException("The stored Freebox credential payload is invalid.");
            }

            return new FreeboxAppCredentials(
                payload.ApplicationId,
                payload.AppToken,
                payload.Status,
                payload.TrackId,
                payload.CreatedAt);
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException(
                "The stored Freebox credential payload is invalid.",
                exception);
        }
    }
}
