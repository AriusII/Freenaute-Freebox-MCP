# syntax=docker/dockerfile:1

ARG SDK_IMAGE=mcr.microsoft.com/dotnet/sdk:11.0.100-resolute-aot
FROM ${SDK_IMAGE} AS build

ARG VERSION=0.1.0-beta
ENV RID=linux-x64

WORKDIR /src
COPY . .

RUN set -eu; \
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
