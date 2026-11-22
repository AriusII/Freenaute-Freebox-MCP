using Microsoft.Extensions.Logging.Abstractions;

namespace Freenaute.Freebox.Mcp.Tools.Services;

[McpServerToolType]
public sealed partial class FreeboxServicesTools
{
    private readonly Func<CancellationToken, Task<FtpConfig>> _getFtpAsync;
    private readonly Func<CancellationToken, Task<TftpConfig>> _getTftpAsync;
    private readonly Func<CancellationToken, Task<UPnpAvConfig>> _getUPnpAvAsync;
    private readonly Func<CancellationToken, Task<SambaConfig>> _getSambaAsync;
    private readonly Func<CancellationToken, Task<AfpConfig>> _getAfpAsync;
    private readonly Func<FtpConfigPatch, CancellationToken, Task<FtpConfig>> _updateFtpAsync;
    private readonly Func<TftpConfigPatch, CancellationToken, Task<TftpConfig>> _updateTftpAsync;
    private readonly Func<UPnpAvConfigPatch, CancellationToken, Task<UPnpAvConfig>> _updateUPnpAvAsync;
    private readonly Func<SambaConfigPatch, CancellationToken, Task<SambaConfig>> _updateSambaAsync;
    private readonly Func<AfpConfigPatch, CancellationToken, Task<AfpConfig>> _updateAfpAsync;
    private readonly ILogger<FreeboxServicesTools> _logger;

    public FreeboxServicesTools(
        IFreeboxClient? client,
        Func<CancellationToken, Task<FtpConfig>>? getFtpAsync = null,
        Func<CancellationToken, Task<TftpConfig>>? getTftpAsync = null,
        Func<CancellationToken, Task<UPnpAvConfig>>? getUPnpAvAsync = null,
        Func<CancellationToken, Task<SambaConfig>>? getSambaAsync = null,
        Func<CancellationToken, Task<AfpConfig>>? getAfpAsync = null,
        Func<FtpConfigPatch, CancellationToken, Task<FtpConfig>>? updateFtpAsync = null,
        Func<TftpConfigPatch, CancellationToken, Task<TftpConfig>>? updateTftpAsync = null,
        Func<UPnpAvConfigPatch, CancellationToken, Task<UPnpAvConfig>>? updateUPnpAvAsync = null,
        Func<SambaConfigPatch, CancellationToken, Task<SambaConfig>>? updateSambaAsync = null,
        Func<AfpConfigPatch, CancellationToken, Task<AfpConfig>>? updateAfpAsync = null,
        ILogger<FreeboxServicesTools>? logger = null)
    {
        _logger = logger ?? NullLogger<FreeboxServicesTools>.Instance;
        if (getFtpAsync is null || getTftpAsync is null || getUPnpAvAsync is null ||
            getSambaAsync is null || getAfpAsync is null ||
            updateFtpAsync is null || updateTftpAsync is null || updateUPnpAvAsync is null ||
            updateSambaAsync is null || updateAfpAsync is null)
        {
            ArgumentNullException.ThrowIfNull(client);
        }

        _getFtpAsync = getFtpAsync ?? (cancellationToken => client!.Services.Ftp.GetConfigurationAsync(cancellationToken));
        _getTftpAsync = getTftpAsync ?? (cancellationToken => client!.Services.Tftp.GetConfigurationAsync(cancellationToken));
        _getUPnpAvAsync = getUPnpAvAsync ??
                          (cancellationToken => client!.Services.UPnpAv.GetConfigurationAsync(cancellationToken));
        _getSambaAsync = getSambaAsync ??
                         (cancellationToken => client!.Services.NetworkShare.GetSambaConfigurationAsync(cancellationToken));
        _getAfpAsync = getAfpAsync ??
                       (cancellationToken => client!.Services.NetworkShare.GetAfpConfigurationAsync(cancellationToken));
        _updateFtpAsync = updateFtpAsync ?? ((patch, cancellationToken) =>
            client!.Services.Ftp.UpdateConfigurationAsync(patch, cancellationToken));
        _updateTftpAsync = updateTftpAsync ?? ((patch, cancellationToken) =>
            client!.Services.Tftp.UpdateConfigurationAsync(patch, cancellationToken));
        _updateUPnpAvAsync = updateUPnpAvAsync ?? ((patch, cancellationToken) =>
            client!.Services.UPnpAv.UpdateConfigurationAsync(patch, cancellationToken));
        _updateSambaAsync = updateSambaAsync ?? ((patch, cancellationToken) =>
            client!.Services.NetworkShare.UpdateSambaConfigurationAsync(patch, cancellationToken));
        _updateAfpAsync = updateAfpAsync ?? ((patch, cancellationToken) =>
            client!.Services.NetworkShare.UpdateAfpConfigurationAsync(patch, cancellationToken));
    }

    [McpServerTool(Name = "freebox_services_status", Title = "Get Freebox services status", ReadOnly = true,
        Idempotent = true, Destructive = false, OpenWorld = true, UseStructuredContent = true,
        OutputSchemaType = typeof(ServicesStatusToolResult))]
    [Description("Returns a bounded service summary for FTP, TFTP, UPnP AV, Samba, and AFP without usernames or domains.")]
    public async Task<ServicesStatusToolResult> GetStatusAsync(CancellationToken cancellationToken)
    {
        LogToolStarted(_logger, "freebox_services_status");
        try
        {
            Task<FtpConfig> ftpTask = _getFtpAsync(cancellationToken);
            Task<TftpConfig> tftpTask = _getTftpAsync(cancellationToken);
            Task<UPnpAvConfig> upnpTask = _getUPnpAvAsync(cancellationToken);
            Task<SambaConfig> sambaTask = _getSambaAsync(cancellationToken);
            Task<AfpConfig> afpTask = _getAfpAsync(cancellationToken);
            await Task.WhenAll(ftpTask, tftpTask, upnpTask, sambaTask, afpTask).ConfigureAwait(false);

            FtpConfig ftp = await ftpTask.ConfigureAwait(false);
            TftpConfig tftp = await tftpTask.ConfigureAwait(false);
            UPnpAvConfig upnp = await upnpTask.ConfigureAwait(false);
            SambaConfig samba = await sambaTask.ConfigureAwait(false);
            AfpConfig afp = await afpTask.ConfigureAwait(false);

            LogToolSucceeded(_logger, "freebox_services_status");
            return new ServicesStatusToolResult(
                new FtpServiceStatusToolResult(ftp.Enabled, ftp.AllowAnonymous, ftp.AllowAnonymousWrite,
                    ftp.AllowRemoteAccess, ftp.WeakPassword, ftp.PortCtrl, ftp.PortData),
                new TftpServiceStatusToolResult(tftp.Enabled, Safe.Text(tftp.Root, 128)),
                new UPnpAvServiceStatusToolResult(upnp.Enabled),
                new SambaServiceStatusToolResult(samba.FileShareEnabled, samba.PrintShareEnabled,
                    samba.LogonEnabled, samba.Smbv2Enabled),
                new AfpServiceStatusToolResult(afp.Enabled, afp.GuestAllow,
                    afp.ServerType?.ToString() is { } serverType ? Safe.Text(serverType, 32) : string.Empty));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            LogToolFailed(_logger, "freebox_services_status", exception.Message);
            return ToolResultFactory.Failure<ServicesStatusToolResult>(FreeboxErrorMapper.Map(exception));
        }
    }

    [McpServerTool(Name = "freebox_services_ftp_update", Title = "Update Freebox FTP service",
        ReadOnly = false, Idempotent = true, Destructive = false, OpenWorld = true, UseStructuredContent = true,
        OutputSchemaType = typeof(ServiceMutationToolResult))]
    [Description("Updates FTP service configuration; passwords and identities are never returned.")]
    public async Task<ServiceMutationToolResult> UpdateFtpAsync(
        FtpConfigPatch patch,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(patch);
        try
        {
            await _updateFtpAsync(patch, cancellationToken).ConfigureAwait(false);
            return Success("ftp", "FTP configuration updated.");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            return ToolResultFactory.Failure<ServiceMutationToolResult>(FreeboxErrorMapper.Map(exception));
        }
    }

    [McpServerTool(Name = "freebox_services_tftp_update", Title = "Update Freebox TFTP service",
        ReadOnly = false, Idempotent = true, Destructive = false, OpenWorld = true, UseStructuredContent = true,
        OutputSchemaType = typeof(ServiceMutationToolResult))]
    [Description("Updates TFTP service configuration.")]
    public async Task<ServiceMutationToolResult> UpdateTftpAsync(
        TftpConfigPatch patch,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(patch);
        try
        {
            await _updateTftpAsync(patch, cancellationToken).ConfigureAwait(false);
            return Success("tftp", "TFTP configuration updated.");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            return ToolResultFactory.Failure<ServiceMutationToolResult>(FreeboxErrorMapper.Map(exception));
        }
    }

    [McpServerTool(Name = "freebox_services_upnpav_update", Title = "Update Freebox UPnP AV service",
        ReadOnly = false, Idempotent = true, Destructive = false, OpenWorld = true, UseStructuredContent = true,
        OutputSchemaType = typeof(ServiceMutationToolResult))]
    [Description("Updates UPnP AV service configuration.")]
    public async Task<ServiceMutationToolResult> UpdateUPnpAvAsync(
        UPnpAvConfigPatch patch,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(patch);
        try
        {
            await _updateUPnpAvAsync(patch, cancellationToken).ConfigureAwait(false);
            return Success("upnp-av", "UPnP AV configuration updated.");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            return ToolResultFactory.Failure<ServiceMutationToolResult>(FreeboxErrorMapper.Map(exception));
        }
    }

    [McpServerTool(Name = "freebox_services_samba_update", Title = "Update Freebox Samba service",
        ReadOnly = false, Idempotent = true, Destructive = false, OpenWorld = true, UseStructuredContent = true,
        OutputSchemaType = typeof(ServiceMutationToolResult))]
    [Description("Updates Samba service configuration; usernames and domains are never returned.")]
    public async Task<ServiceMutationToolResult> UpdateSambaAsync(
        SambaConfigPatch patch,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(patch);
        try
        {
            await _updateSambaAsync(patch, cancellationToken).ConfigureAwait(false);
            return Success("samba", "Samba configuration updated.");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            return ToolResultFactory.Failure<ServiceMutationToolResult>(FreeboxErrorMapper.Map(exception));
        }
    }

    [McpServerTool(Name = "freebox_services_afp_update", Title = "Update Freebox AFP service",
        ReadOnly = false, Idempotent = true, Destructive = false, OpenWorld = true, UseStructuredContent = true,
        OutputSchemaType = typeof(ServiceMutationToolResult))]
    [Description("Updates AFP service configuration; usernames are never returned.")]
    public async Task<ServiceMutationToolResult> UpdateAfpAsync(
        AfpConfigPatch patch,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(patch);
        try
        {
            await _updateAfpAsync(patch, cancellationToken).ConfigureAwait(false);
            return Success("afp", "AFP configuration updated.");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            return ToolResultFactory.Failure<ServiceMutationToolResult>(FreeboxErrorMapper.Map(exception));
        }
    }

    private ServiceMutationToolResult Success(string service, string message)
    {
        LogToolMutationSucceeded(_logger, "freebox_services_update");
        return new ServiceMutationToolResult(true, service, message);
    }

    [LoggerMessage(EventId = 2200, Level = LogLevel.Information, Message = "Executing {ToolName}.")]
    private static partial void LogToolStarted(ILogger logger, string toolName);

    [LoggerMessage(EventId = 2201, Level = LogLevel.Warning, Message = "{ToolName} failed: {Reason}")]
    private static partial void LogToolFailed(ILogger logger, string toolName, string? reason);

    [LoggerMessage(EventId = 2202, Level = LogLevel.Information, Message = "{ToolName} succeeded.")]
    private static partial void LogToolSucceeded(ILogger logger, string toolName);

    [LoggerMessage(EventId = 2203, Level = LogLevel.Information,
        Message = "{ToolName} mutation succeeded.")]
    private static partial void LogToolMutationSucceeded(ILogger logger, string toolName);
}
