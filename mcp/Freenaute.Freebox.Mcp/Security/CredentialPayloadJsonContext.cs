namespace Freenaute.Freebox.Mcp.Security;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UseStringEnumConverter = true,
    GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(CredentialPayload))]
internal sealed partial class CredentialPayloadJsonContext : JsonSerializerContext;
