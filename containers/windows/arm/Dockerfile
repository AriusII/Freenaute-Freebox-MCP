# syntax=docker/dockerfile:1
# escape=`

ARG SDK_IMAGE=mcr.microsoft.com/dotnet/sdk:11.0.100-nanoserver-ltsc2025
ARG TOOLCHAIN_IMAGE=mcr.microsoft.com/windows/servercore:ltsc2025-arm64
ARG BASE_IMAGE=mcr.microsoft.com/windows/nanoserver:ltsc2025-arm64

FROM ${SDK_IMAGE} AS sdk
FROM ${TOOLCHAIN_IMAGE} AS build

ARG VERSION=1.0.0
ARG BUILD_CONFIGURATION=Release
ENV RID=win-arm64 `
    VERSION=$VERSION `
    BUILD_CONFIGURATION=$BUILD_CONFIGURATION

SHELL ["powershell.exe", "-NoProfile", "-Command", "$ErrorActionPreference = 'Stop';"]

COPY --from=sdk ["C:/Program Files/dotnet/", "C:/Program Files/dotnet/"]
ENV PATH="C:\Program Files\dotnet;C:\Windows\System32\WindowsPowerShell\v1.0;C:\Windows\system32;C:\Windows;C:\Windows\System32\Wbem" `
    DOTNET_NOLOGO=1 `
    NUGET_XMLDOC_MODE=skip

RUN $bootstrapper = 'C:\vs_buildtools.exe'; \
    Invoke-WebRequest -UseBasicParsing -Uri 'https://aka.ms/vs/17/release/vs_buildtools.exe' -OutFile $bootstrapper; \
    $process = Start-Process -FilePath $bootstrapper -ArgumentList '--quiet','--wait','--norestart','--nocache','--installPath','C:\BuildTools','--add','Microsoft.VisualStudio.Component.VC.Tools.x86.x64','--add','Microsoft.VisualStudio.Component.VC.Tools.ARM64','--add','Microsoft.VisualStudio.Component.Windows11SDK.26100' -Wait -PassThru; \
    if ($process.ExitCode -notin @(0, 3010)) { throw "Visual Studio Build Tools install failed with exit code $($process.ExitCode)." }; \
    Remove-Item -Force $bootstrapper

WORKDIR C:/src
COPY . .

RUN dotnet restore mcp/Freenaute.Freebox.Mcp/Freenaute.Freebox.Mcp.csproj --runtime $env:RID; \
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }; \
    dotnet publish mcp/Freenaute.Freebox.Mcp/Freenaute.Freebox.Mcp.csproj -c $env:BUILD_CONFIGURATION -r $env:RID --self-contained true --no-restore -p:PublishAot=true -p:PublishSingleFile=true -p:InvariantGlobalization=true -p:Version=$env:VERSION -o C:\app\publish; \
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }; \
    $executable = 'C:\app\publish\Freenaute.Freebox.Mcp.exe'; \
    $bytes = [IO.File]::ReadAllBytes($executable); \
    $peOffset = [BitConverter]::ToUInt32($bytes, 0x3c); \
    $machine = [BitConverter]::ToUInt16($bytes, $peOffset + 4); \
    if ($machine -ne 0xaa64) { throw "PE machine 0x$($machine.ToString('X4')) does not match RID 'win-arm64' (expected 0xAA64)." }

FROM ${BASE_IMAGE} AS final

ARG VERSION=1.0.0

LABEL org.opencontainers.image.title="Freenaute Freebox MCP" `
      org.opencontainers.image.description="Local stdio MCP server for Freebox discovery, pairing coordination, and system/network status (C#/.NET 11 Native AOT, single file)." `
      org.opencontainers.image.source="https://github.com/AriusII/Freenaute-Freebox-MCP" `
      org.opencontainers.image.licenses="MIT" `
      org.opencontainers.image.version="${VERSION}"

WORKDIR C:/app
COPY --from=build ["C:/app/publish/Freenaute.Freebox.Mcp.exe", "C:/app/"]
ENTRYPOINT ["C:\\app\\Freenaute.Freebox.Mcp.exe"]
