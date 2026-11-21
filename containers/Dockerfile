# syntax=docker/dockerfile:1
#
# Freenaute-Freebox-MCP — Linux OCI image (stdio MCP server; C# 15 / .NET 11,
# Native AOT, single-file, self-contained, non-root).
#
# Build one platform (default linux-x64):
#   docker build -f containers/Dockerfile -t freenaute-freebox-mcp:local .
#
# Multi-arch manifest list with buildx (TARGETARCH drives the RID:
# amd64 -> linux-x64, arm64 -> linux-arm64):
#   docker buildx build --platform linux/amd64,linux/arm64 \
#     -f containers/Dockerfile \
#     -t ghcr.io/freenaute/freenaute-freebox-mcp:0.1.0-beta --push .
#
# Explicit RID override (legacy builder, where TARGETARCH stays empty):
#   docker build --build-arg RID=linux-arm64 \
#     -f containers/Dockerfile -t freenaute-freebox-mcp:arm64 .
#
# Run: this is a stdio MCP server — attach stdin, NEVER allocate a TTY (-t):
#   docker run -i --rm freenaute-freebox-mcp:local

# ---------------------------------------------------------------------------
# Build stage: .NET 11 SDK image that ships the Native AOT toolchain
# (clang/lld). Tag verified on MCR on 2026-11-21: 11.0.100-resolute-aot is a
# multi-arch manifest (linux/amd64 + linux/arm64).
# ---------------------------------------------------------------------------
ARG SDK_IMAGE=mcr.microsoft.com/dotnet/sdk:11.0.100-resolute-aot
FROM ${SDK_IMAGE} AS build

ARG TARGETARCH
ARG RID=linux-x64
ARG VERSION=0.1.0-beta

WORKDIR /src
# The build context is trimmed by .dockerignore (.git, .github, .idea, tests,
# containers, eng, .artifacts, bin/obj, *.pem/*.key/*.pfx/*.p12, .env*, ...).
COPY . .

RUN set -e; \
    if [ -n "${TARGETARCH}" ]; then \
      case "${TARGETARCH}" in \
        amd64) RID=linux-x64 ;; \
        arm64) RID=linux-arm64 ;; \
        *) echo "Unsupported TARGETARCH '${TARGETARCH}'; expected amd64 or arm64." >&2; exit 1 ;; \
      esac; \
    fi; \
    case "${RID}" in \
      linux-x64|linux-arm64) ;; \
      *) echo "Unsupported RID '${RID}'; whitelist: linux-x64, linux-arm64." >&2; exit 1 ;; \
    esac; \
    echo "Publishing Native AOT single-file for RID=${RID} (TARGETARCH='${TARGETARCH}')"; \
    dotnet restore mcp/Freenaute.Freebox.Mcp/Freenaute.Freebox.Mcp.csproj --runtime "${RID}" && \
    dotnet publish mcp/Freenaute.Freebox.Mcp/Freenaute.Freebox.Mcp.csproj \
      -c Release \
      -r "${RID}" \
      --self-contained true \
      --no-restore \
      -p:PublishAot=true \
      -p:PublishSingleFile=true \
      -p:InvariantGlobalization=true \
      -p:Version="${VERSION}" \
      -o /app/publish

# ---------------------------------------------------------------------------
# Runtime stage: chiseled runtime-deps (no shell, no package manager, non-root
# UID 1654), glibc-based Ubuntu Resolute.
# Tag verified on MCR on 2026-11-21: 11.0-resolute-chiseled is a multi-arch
# manifest (linux/amd64 + linux/arm64). No `alpine-chiseled` tag exists on MCR
# (checked the same day), and the supported perimeter is glibc linux-x64 /
# linux-arm64, so resolute-chiseled is kept on purpose: it is the lightest MCR
# base that still provides glibc and CA certificates, which the Native AOT
# binary needs, while staying shell-free and non-root. `scratch` is not used.
# ---------------------------------------------------------------------------
ARG RUNTIME_IMAGE=mcr.microsoft.com/dotnet/runtime-deps:11.0-resolute-chiseled
FROM ${RUNTIME_IMAGE} AS final

ARG VERSION=0.1.0-beta

LABEL org.opencontainers.image.title="Freenaute Freebox MCP" \
      org.opencontainers.image.description="Local stdio MCP server for Freebox discovery, pairing coordination, and system/network status (C#/.NET 11 Native AOT, single file)." \
      org.opencontainers.image.source="https://github.com/freenaute/Freenaute-Freebox-MCP" \
      org.opencontainers.image.licenses="MIT" \
      org.opencontainers.image.version="${VERSION}"

WORKDIR /app
COPY --from=build /app/publish/Freenaute.Freebox.Mcp /app/
USER 1654
ENTRYPOINT ["/app/Freenaute.Freebox.Mcp"]
