namespace Freenaute.Freebox.Mcp.Tools.System;

public sealed record StandbyStatusToolResult(
    bool? PlanningEnabled,
    string PlanningMode,
    string[] AvailablePlanningModes,
    long? NextChangeUnixSeconds,
    long? ResolutionSeconds,
    int MappingSlots,
    int ActiveMappingSlots);
