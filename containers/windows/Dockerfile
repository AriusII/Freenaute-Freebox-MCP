# syntax=docker/dockerfile:1
#
# Freenaute-Freebox-MCP — Windows OCI image (Windows containers ONLY).
# Windows container images can be built and run exclusively on a Windows
# host / CI runner (e.g. GitHub runners `windows-latest` for win-x64 and
# `windows-11-arm` for win-arm64). A Linux builder cannot produce them.
#
# win-x64 (default):
#   docker build -f containers/windows/Dockerfile -t freenaute-freebox-mcp:win-x64 .
#
# win-arm64 — the final base tag is verified on MCR on 2026-11-21:
#   mcr.microsoft.com/windows/nanoserver:ltsc2025-arm64 (windows/arm64 manifest exists).
#   NOTE: no arm64 variant of the .NET SDK image exists on MCR for .NET 11
#   (dotnet/sdk:11.0.100-nanoserver-ltsc2025 is a windows/amd64-only manifest
#   and dotnet/sdk publishes no *arm64* Windows tag at all). For a win-arm64
#   build, either run the amd64 SDK image under x64 emulation on a Windows 11
#   ARM 24H2+ host, or cross-publish win-arm64 from an amd64 Windows runner
#   (the Native AOT compiler cross-compiles to arm64) while only the final
#   stage uses the arm64 base:
#   docker build -f containers/windows/Dockerfile \
#     --build-arg BASE_IMAGE=mcr.microsoft.com/windows/nanoserver:ltsc2025-arm64 \
#     --build-arg RID=win-arm64 \
#     -t freenaute-freebox-mcp:win-arm64 .
#
# Run: stdio MCP server — attach stdin, NEVER -t:
#   docker run -i --rm freenaute-freebox-mcp:win-x64

# Build stage: .NET 11 SDK on Windows Server Core LTSC 2025 Nano Server.
# Tag verified on MCR on 2026-11-21: windows/amd64 manifest.
ARG SDK_IMAGE=mcr.microsoft.com/dotnet/sdk:11.0.100-nanoserver-ltsc2025
# Final stage: Windows Nano Server LTSC 2025 (windows/amd64). Override with
# mcr.microsoft.com/windows/nanoserver:ltsc2025-arm64 for win-arm64.
ARG BASE_IMAGE=mcr.microsoft.com/windows/nanoserver:ltsc2025
FROM ${SDK_IMAGE} AS build

ARG TARGETARCH
ARG RID=win-x64
ARG VERSION=0.1.0-beta

WORKDIR C:/src
# Same trimmed build context as the Linux image (.dockerignore at repo root).
COPY . .

# The SDK nanoserver image ships PowerShell (C:\Program Files\powershell on
# PATH), used here to map TARGETARCH -> RID with a whitelist check.
SHELL ["powershell.exe", "-NoProfile", "-Command", "$ErrorActionPreference = 'Stop';"]
RUN $rid = $env:RID; $t = $env:TARGETARCH; if (-not [string]::IsNullOrEmpty($t)) { if ($t -eq 'amd64') { $rid = 'win-x64' } elseif ($t -eq 'arm64') { $rid = 'win-arm64' } else { Write-Error "Unsupported TARGETARCH '$t'; expected amd64 or arm64."; exit 1 } }; if (($rid -ne 'win-x64') -and ($rid -ne 'win-arm64')) { Write-Error "Unsupported RID '$rid'; whitelist: win-x64, win-arm64."; exit 1 }; Write-Host "Publishing Native AOT single-file for RID=$rid (TARGETARCH='$t')"; dotnet restore mcp/Freenaute.Freebox.Mcp/Freenaute.Freebox.Mcp.csproj --runtime $rid; if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }; dotnet publish mcp/Freenaute.Freebox.Mcp/Freenaute.Freebox.Mcp.csproj -c Release -r $rid --self-contained true --no-restore -p:PublishAot=true -p:PublishSingleFile=true -p:InvariantGlobalization=true -p:Version=$env:VERSION -o C:\app\publish; if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

FROM ${BASE_IMAGE} AS final

ARG VERSION=0.1.0-beta

LABEL org.opencontainers.image.title="Freenaute Freebox MCP" \
      org.opencontainers.image.description="Local stdio MCP server for Freebox discovery, pairing coordination, and system/network status (C#/.NET 11 Native AOT, single file)." \
      org.opencontainers.image.source="https://github.com/freenaute/Freenaute-Freebox-MCP" \
      org.opencontainers.image.licenses="MIT" \
      org.opencontainers.image.version="${VERSION}"

WORKDIR C:/app
COPY --from=build C:/app/publish/Freenaute.Freebox.Mcp.exe C:/app/
# Windows containers have no POSIX UID model; Nano Server runs the process as
# the container's default (non-admin-host) user. There is intentionally no
# USER instruction here.
ENTRYPOINT ["C:\\app\\Freenaute.Freebox.Mcp.exe"]
