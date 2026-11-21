namespace Freenaute.Freebox.Mcp.Core.Serialization;

[JsonSourceGenerationOptions(
    GenerationMode = JsonSourceGenerationMode.Metadata,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UseStringEnumConverter = true,
    WriteIndented = false)]
[JsonSerializable(typeof(FreeboxCoreError))]
[JsonSerializable(typeof(FreeboxCoreErrorCode))]
[JsonSerializable(typeof(FreeboxCredentialState))]
[JsonSerializable(typeof(FreeboxPairingResult))]
[JsonSerializable(typeof(FreeboxCredentialResult))]
[JsonSerializable(typeof(FreeboxConnectionResult))]
[JsonSerializable(typeof(FreeboxDiscoverySnapshot))]
[JsonSerializable(typeof(FreeboxConnectionSnapshot))]
[JsonSerializable(typeof(FreeboxLanInterfaceSnapshot))]
[JsonSerializable(typeof(FreeboxLanHostSnapshot))]
[JsonSerializable(typeof(FreeboxLanSnapshot))]
[JsonSerializable(typeof(FreeboxWifiAccessPointSnapshot))]
[JsonSerializable(typeof(FreeboxWifiBssSnapshot))]
[JsonSerializable(typeof(FreeboxWifiSnapshot))]
[JsonSerializable(typeof(FreeboxNetworkSnapshot))]
[JsonSerializable(typeof(FreeboxSystemSensorSnapshot))]
[JsonSerializable(typeof(FreeboxSystemExpansionSnapshot))]
[JsonSerializable(typeof(FreeboxSystemSnapshot))]
[JsonSerializable(typeof(FreeboxCoreResult<bool>))]
[JsonSerializable(typeof(FreeboxCoreResult<FreeboxPairingResult>))]
[JsonSerializable(typeof(FreeboxCoreResult<FreeboxCredentialResult>))]
[JsonSerializable(typeof(FreeboxCoreResult<FreeboxConnectionResult>))]
[JsonSerializable(typeof(FreeboxCoreResult<FreeboxDiscoverySnapshot>))]
[JsonSerializable(typeof(FreeboxCoreResult<FreeboxNetworkSnapshot>))]
[JsonSerializable(typeof(FreeboxCoreResult<FreeboxSystemSnapshot>))]
public sealed partial class FreeboxCoreJsonContext : JsonSerializerContext;
