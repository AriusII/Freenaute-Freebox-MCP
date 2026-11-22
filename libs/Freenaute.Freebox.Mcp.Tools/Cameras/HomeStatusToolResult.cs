namespace Freenaute.Freebox.Mcp.Tools.Cameras;

public sealed record HomeStatusToolResult(
    string CurrentLanguage,
    string[] AvailableLanguages,
    bool? StandbyPlanningEnabled,
    string StandbyPlanningMode,
    string[] AvailableStandbyPlanningModes,
    long? NextStandbyChangeUnixSeconds,
    long? StandbyResolutionSeconds,
    string UpdateState,
    string UpgradeState,
    long? UpgradePercent);
