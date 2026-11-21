namespace Freenaute.Freebox.Mcp.Core.Tests.Security;

public sealed class CredentialStoreHostTests
{
    private const string ExpectedKeyPrefix = "org.freenaute.freebox-mcp/credentials:";

    [Fact]
    public void CredentialStoreKey_IsStableAndScopesAllIdentityComponents()
    {
        var baseline = new FreeboxCredentialId("profile", "application", "freebox-uid-1");
        string key = CredentialStoreKey.For(baseline);

        Assert.Equal(key, CredentialStoreKey.For(new FreeboxCredentialId("profile", "application", "freebox-uid-1")));
        Assert.NotEqual(key, CredentialStoreKey.For(new FreeboxCredentialId("profile", "application", "freebox-uid-2")));
        Assert.NotEqual(key, CredentialStoreKey.For(new FreeboxCredentialId("profile", "application-two", "freebox-uid-1")));
        Assert.NotEqual(key, CredentialStoreKey.For(new FreeboxCredentialId("profile-two", "application", "freebox-uid-1")));
        Assert.DoesNotContain("profile", key, StringComparison.Ordinal);
        Assert.DoesNotContain("application", key, StringComparison.Ordinal);
        Assert.DoesNotContain("freebox-uid-1", key, StringComparison.Ordinal);
    }

    [Fact]
    public void CredentialStoreKey_UsesFreenauteOrgPrefixAndLowercaseSha256Suffix()
    {
        var id = new FreeboxCredentialId("default", "org.freenaute.freebox-mcp", "freebox-uid-1");
        string key = CredentialStoreKey.For(id);

        Assert.StartsWith(ExpectedKeyPrefix, key, StringComparison.Ordinal);
        Assert.DoesNotContain("io.github", key, StringComparison.Ordinal);
        string suffix = key[ExpectedKeyPrefix.Length..];
        Assert.Equal(64, suffix.Length);
        Assert.Matches("^[0-9a-f]{64}$", suffix);
    }

    [Fact]
    public void CredentialStoreKey_ScopesSha256OverLengthPrefixedIdentityComponents()
    {
        var id = new FreeboxCredentialId("default", "org.freenaute.freebox-mcp", "uid-42");
        string freeboxUid = id.FreeboxUid ?? string.Empty;
        string applicationId = id.ApplicationId ?? string.Empty;
        string profile = id.Profile ?? string.Empty;
        string scope = $"{freeboxUid.Length}:{freeboxUid}{applicationId.Length}:{applicationId}{profile.Length}:{profile}";
        string expectedSuffix = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(scope)));

        Assert.Equal(ExpectedKeyPrefix + expectedSuffix, CredentialStoreKey.For(id));
    }

    [Fact]
    public void CredentialStoreKey_RejectsNullIdentity()
    {
        Assert.Throws<ArgumentNullException>(() => CredentialStoreKey.For(null!));
    }

    [Fact]
    public async Task UnsupportedStorage_IsAvailableFalseAndFailsClosedOnEveryOperation()
    {
        var storage = new UnsupportedCredentialStorage();
        var id = new FreeboxCredentialId("profile", "application", "freebox");
        var credentials = new FreeboxAppCredentials(
            "application", "secret-token", FreeboxCredentialStatus.Approved, 1, DateTimeOffset.UtcNow);

        Assert.False(storage.IsAvailable);
        Assert.Equal("unavailable", storage.Kind);
        await Assert.ThrowsAsync<NotSupportedException>(async () => await storage.GetAsync(
            id, TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<NotSupportedException>(async () => await storage.PutAsync(
            id, credentials, TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<NotSupportedException>(async () => await storage.DeleteAsync(
            id, TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData("1")]
    [InlineData("true")]
    [InlineData("TRUE")]
    public void PairingPolicyParser_AcceptsOnlyExactEnabledValues(string value)
    {
        Assert.True(PairingPolicy.IsEnabled(value));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("0")]
    [InlineData("2")]
    [InlineData("on")]
    [InlineData("false")]
    [InlineData("False")]
    [InlineData("yes")]
    [InlineData(" true")]
    [InlineData("true ")]
    [InlineData("True")]
    [InlineData("TRUE\n")]
    public void PairingPolicyParser_RejectsNonExactValues(string? value)
    {
        Assert.False(PairingPolicy.IsEnabled(value));
    }

    [Theory]
    [InlineData("{not-json")]
    [InlineData("null")]
    [InlineData("{}")]
    [InlineData("{\"FormatVersion\":2,\"ApplicationId\":\"app\",\"AppToken\":\"secret\"}")]
    [InlineData("{\"FormatVersion\":1,\"ApplicationId\":\"\",\"AppToken\":\"secret\"}")]
    [InlineData("{\"FormatVersion\":1,\"ApplicationId\":\"app\",\"AppToken\":\"\"}")]
    public void CorruptOrVersionedPayload_IsRejectedWithoutLeakingToken(string json)
    {
        InvalidDataException exception = Assert.Throws<InvalidDataException>(() => CredentialPayloadCodec.Deserialize(
            Encoding.UTF8.GetBytes(json)));

        Assert.Equal("The stored Freebox credential payload is invalid.", exception.Message);
        Assert.DoesNotContain("secret", exception.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void ValidVersionedPayload_RoundTripsButTokenRemainsOutsidePublicResults()
    {
        var credentials = new FreeboxAppCredentials(
            "application",
            "secret-token",
            FreeboxCredentialStatus.Approved,
            42,
            DateTimeOffset.Parse("2026-01-02T03:04:05Z", CultureInfo.InvariantCulture));

        byte[] json = CredentialPayloadCodec.Serialize(credentials);
        FreeboxAppCredentials restored = Assert.IsType<FreeboxAppCredentials>(CredentialPayloadCodec.Deserialize(json));

        Assert.Equal(credentials.ApplicationId, restored.ApplicationId);
        Assert.Equal(credentials.GetAppToken(), restored.GetAppToken());
        Assert.Equal(credentials.Status, restored.Status);
        Assert.Equal(credentials.TrackId, restored.TrackId);
        Assert.Equal(credentials.CreatedAt, restored.CreatedAt);
    }
}
