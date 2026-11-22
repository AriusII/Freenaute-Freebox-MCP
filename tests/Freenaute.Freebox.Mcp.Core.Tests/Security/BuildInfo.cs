namespace Freenaute.Freebox.Mcp;

/// <summary>
///     Shim mirroring the host's generated <c>BuildInfo</c>
///     (mcp/Freenaute.Freebox.Mcp/obj/.../Generated/BuildInfo.g.cs, produced by the GenerateBuildInfo
///     target from MSBuild properties). The host Security sources recompiled into this test assembly
///     reference <c>BuildInfo</c>, so the constants must exist here too. Keep in sync with
///     <c>Freenaute.Freebox.Mcp.csproj</c> (<c>$(Version)</c> and the org.freenaute application id).
/// </summary>
internal static class BuildInfo
{
    public const string Version = "0.1.0-beta";
    public const string ApplicationId = "org.freenaute.freebox-mcp";
}
