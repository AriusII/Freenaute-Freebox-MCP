namespace Freenaute.Freebox.Mcp.Tools.Serialization;

[JsonSerializable(typeof(DiscoveryToolResult))]
[JsonSerializable(typeof(PairingStatusToolResult))]
[JsonSerializable(typeof(PairingInitiationToolResult))]
[JsonSerializable(typeof(PairingApprovalToolResult))]
[JsonSerializable(typeof(SystemToolResult))]
[JsonSerializable(typeof(ConnectionToolResult))]
[JsonSerializable(typeof(LanToolResult))]
[JsonSerializable(typeof(WifiToolResult))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    UseStringEnumConverter = true)]
public sealed partial class ToolResultJsonSerializerContext : JsonSerializerContext;
