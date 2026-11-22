namespace Freenaute.Freebox.Mcp.Tools.System;

public sealed record StandbyConfigurationToolResult(
    bool? PlanningEnabled,
    string PlanningMode,
    long? ResolutionSeconds,
    int MappingSlots,
    int ActiveMappingSlots);
