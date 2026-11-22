namespace Freenaute.Freebox.Mcp.Tools.System;

public sealed record SystemUpdatesStatusToolResult(
    string State,
    string UpgradeState,
    long? UpgradePercent,
    string OldVersion,
    string NewVersion);
