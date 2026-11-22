namespace Freenaute.Freebox.Mcp.Tools.AirMedia;

public sealed record AirMediaReceiverToolResult(
    string Name,
    bool PasswordProtected,
    bool CanDisplayPhotos,
    bool CanPlayAudio,
    bool CanPlayVideo,
    bool CanDisplayScreen);
