# 🚀 Freenaute Freebox MCP

[![CI](https://img.shields.io/github/actions/workflow/status/AriusII/Freenaute-Freebox-MCP/ci.yml?label=CI)](https://github.com/AriusII/Freenaute-Freebox-MCP/actions/workflows/ci.yml)
[![Release](https://img.shields.io/github/actions/workflow/status/AriusII/Freenaute-Freebox-MCP/release.yml?label=Release)](https://github.com/AriusII/Freenaute-Freebox-MCP/actions/workflows/release.yml)
[![NuGet](https://img.shields.io/nuget/v/Freenaute.Freebox.Mcp)](https://www.nuget.org/packages/Freenaute.Freebox.Mcp)
[![.NET](https://img.shields.io/badge/.NET-11.0-512BD4)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](./LICENSE)

> A local **Model Context Protocol** server for **Freebox OS** built with **C# 15 / .NET 11** and designed for **Native AOT**.

## 🎯 Mission

Provide a robust MCP integration for Freebox Server with:

- ✅ strict local transport (`stdio`)
- ✅ secure pairing workflow with physical validation
- ✅ technical observability through structured logs
- ✅ safe, bounded, sanitized outputs for LLM tools
- ✅ modern CI/CD and container packaging

## ✨ What this repository provides

- **Local MCP server** (`stdio` transport only) for Freebox Server integration.
- **57 tools** across discovery, pairing, network (basic + advanced), files, system (basic + home controls), downloads, calls, contacts, cameras, notifications, AirMedia, shares, services, and parental controls.
- **Live + static MCP resources** in **English and French**.
- **Secure credential handling** (OS vaults and encrypted file mode), no plaintext token fallback.
- **Cross-platform artifacts** for:
  - Windows `x64`, `arm64`
  - Linux `x64`, `arm64`
- **Four dedicated Dockerfiles** and a CI/CD pipeline for binary releases + GHCR images.

## 🧭 Architecture

```text
.
├── mcp/Freenaute.Freebox.Mcp/              # MCP host (Program, startup, security, packaging manifest)
├── libs/Freenaute.Freebox.Mcp.Core/        # DI, orchestrator, auth/bootstrap, storage abstraction, redaction
├── libs/Freenaute.Freebox.Mcp.Tools/       # MCP tools (57) + bounded/sanitized result models
├── libs/Freenaute.Freebox.Mcp.Resources/   # MCP docs resources (EN/FR domain catalogs) + live resources
├── containers/                             # linux/windows Dockerfiles (x64 + arm64)
├── tests/                                  # Core / Tools / Resources / Protocol tests
└── .github/workflows/                      # CI + Release pipelines
```

## 🛠️ Tooling and runtime

- Target framework: `net11.0`
- Language version: `C# 15`
- AOT/trimming posture: `IsAotCompatible`, `IsTrimmable`, source-generated JSON contexts
- SDK pinned in [global.json](./global.json)
- Main Freebox API client: [`Freenaute.Freebox.Client`](https://www.nuget.org/packages/Freenaute.Freebox.Client)

## 🔐 Security model (high level)

- Pairing requires **physical approval** on the Freebox display.
- Session token is memory-only; app token is persisted only in secure storage.
- No CLI arguments accepted by the server.
- Structured outputs are sanitized and bounded (no raw IP/MAC/SSID/token leakage).

Details: [Security README](./mcp/Freenaute.Freebox.Mcp/Security/README.md)

## 🧪 Local build and test

```powershell
dotnet restore Freenaute.Freebox.MCP.slnx
dotnet build   Freenaute.Freebox.MCP.slnx -c Release --no-restore
dotnet test    Freenaute.Freebox.MCP.slnx -c Release --no-build
```

Native AOT publish (example `win-x64`):

```powershell
dotnet publish mcp/Freenaute.Freebox.Mcp/Freenaute.Freebox.Mcp.csproj `
  -c Release -r win-x64 --self-contained true `
  -p:PublishAot=true -p:PublishSingleFile=true -p:InvariantGlobalization=true `
  -o .artifacts/publish/1.0.0/win-x64
```

## 📦 CI/CD overview

- [CI workflow](./.github/workflows/ci.yml):
  - restore/build/test
  - metadata and Docker policy validation
  - Native AOT smoke publish (`linux-x64`)
- [Release workflow](./.github/workflows/release.yml):
  - release binaries (`win-x64`, `win-arm64`, `linux-x64`, `linux-arm64`)
  - NuGet package publish (`Freenaute.Freebox.Mcp`)
  - Linux container publish (amd64 + arm64 + multi-arch manifest)
  - optional Windows container publish (amd64/arm64 on manual dispatch when Docker is available on runner)
  - SBOM and checksums
  - GitHub release assets

## 🐳 Containers

Container documentation and commands:

- [containers/README.md](./containers/README.md)

## 📚 Freebox and MCP references

- Freebox OS SDK: https://dev.freebox.fr/sdk/os/
- Freebox login/auth model: https://dev.freebox.fr/sdk/os/login/
- MCP specification: https://spec.modelcontextprotocol.io/
- MCP .NET SDK: https://csharp.sdk.modelcontextprotocol.io/
- NuGet MCP server guide: https://aka.ms/nuget/mcp/guide

---

💡 If you want to integrate Freebox capabilities into MCP clients cleanly and safely, this repository is designed for that exact purpose.
