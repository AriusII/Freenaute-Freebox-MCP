namespace Freenaute.Freebox.Mcp.Tools.Cameras;

public sealed record CameraToolResult(
    string CameraId,
    long? NodeId,
    bool StreamAvailable,
    bool LanBound);
