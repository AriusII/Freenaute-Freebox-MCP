namespace Freenaute.Freebox.Mcp.Security;

internal static class CredentialStoreKey
{
    internal const string ServicePrefix = BuildInfo.ApplicationId;

    public static string For(FreeboxCredentialId id)
    {
        ArgumentNullException.ThrowIfNull(id);

        string freeboxUid = id.FreeboxUid ?? string.Empty;
        string applicationId = id.ApplicationId ?? string.Empty;
        string profile = id.Profile ?? string.Empty;
        byte[] scope = Encoding.UTF8.GetBytes(
            $"{freeboxUid.Length}:{freeboxUid}{applicationId.Length}:{applicationId}{profile.Length}:{profile}");
        try
        {
            byte[] hash = SHA256.HashData(scope);
            try
            {
                return $"{ServicePrefix}/credentials:{Convert.ToHexStringLower(hash)}";
            }
            finally
            {
                CryptographicOperations.ZeroMemory(hash);
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(scope);
        }
    }
}
