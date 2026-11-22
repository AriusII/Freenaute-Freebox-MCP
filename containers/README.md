# Container images — Freenaute-Freebox-MCP

This repository now uses **specialized Dockerfiles per OS and architecture**:

- `containers/linux/x64/Dockerfile` → `linux-x64`
- `containers/linux/arm/Dockerfile` → `linux-arm64`
- `containers/windows/x64/Dockerfile` → `win-x64`
- `containers/windows/arm/Dockerfile` → `win-arm64`

The MCP server remains local/stdin-stdout only (no HTTP port).

## Build locally

Linux x64:

```powershell
docker build -f containers/linux/x64/Dockerfile -t freenaute-freebox-mcp:linux-amd64 .
```

Linux ARM64:

```powershell
docker build -f containers/linux/arm/Dockerfile -t freenaute-freebox-mcp:linux-arm64 .
```

Windows x64:

```powershell
docker build -f containers/windows/x64/Dockerfile -t freenaute-freebox-mcp:windows-amd64 .
```

Windows ARM64:

```powershell
docker build -f containers/windows/arm/Dockerfile -t freenaute-freebox-mcp:windows-arm64 .
```

## Run

```powershell
docker run -i --rm freenaute-freebox-mcp:linux-amd64
```

Use `-i` (stdio MCP protocol). Do not publish ports.

## CI/CD container flow

The release workflow:

1. Builds and pushes:
    - `ghcr.io/<owner>/<repo>:linux-amd64`
    - `ghcr.io/<owner>/<repo>:linux-arm64`
    - `ghcr.io/<owner>/<repo>:windows-amd64`
    - `ghcr.io/<owner>/<repo>:windows-arm64`
2. Creates/pushes Linux multi-arch manifests:
    - `ghcr.io/<owner>/<repo>:<version>`
    - `ghcr.io/<owner>/<repo>:latest`
      (both include linux amd64 + arm64)

## Validation

Static rules are enforced by:

```powershell
dotnet msbuild -t:ValidateContainers
```

Validation checks include:

- all 4 Dockerfiles exist
- MCR base images only
- Native AOT publish flags
- Linux non-root user (`USER 1654`)
- architecture-specific RID markers
- no secret-like `ENV`/`ARG` lines
