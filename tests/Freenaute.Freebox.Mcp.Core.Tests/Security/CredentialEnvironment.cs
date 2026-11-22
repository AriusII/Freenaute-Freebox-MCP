namespace Freenaute.Freebox.Mcp.Core.Tests.Security;

/// <summary>
///     Serializes tests that mutate the process-wide FREEBOX_CREDENTIAL_* environment variables.
/// </summary>
[CollectionDefinition(Name)]
public sealed class CredentialEnvironment
{
    public const string Name = "CredentialEnvironment";
}
