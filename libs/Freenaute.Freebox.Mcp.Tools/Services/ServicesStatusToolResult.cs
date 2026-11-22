namespace Freenaute.Freebox.Mcp.Tools.Services;

public sealed record ServicesStatusToolResult(
    FtpServiceStatusToolResult Ftp,
    TftpServiceStatusToolResult Tftp,
    UPnpAvServiceStatusToolResult UPnpAv,
    SambaServiceStatusToolResult Samba,
    AfpServiceStatusToolResult Afp);

public sealed record FtpServiceStatusToolResult(
    bool? Enabled,
    bool? AllowAnonymous,
    bool? AllowAnonymousWrite,
    bool? AllowRemoteAccess,
    bool? WeakPassword,
    long? PortCtrl,
    long? PortData);

public sealed record TftpServiceStatusToolResult(
    bool? Enabled,
    string Root);

public sealed record UPnpAvServiceStatusToolResult(bool? Enabled);

public sealed record SambaServiceStatusToolResult(
    bool? FileShareEnabled,
    bool? PrintShareEnabled,
    bool? LogonEnabled,
    bool? Smbv2Enabled);

public sealed record AfpServiceStatusToolResult(
    bool? Enabled,
    bool? GuestAllow,
    string ServerType);
