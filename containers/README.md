# Containers (Windows + Linux, x64 + ARM64)

This repository ships **four architecture-specific Dockerfiles**:

- [containers/linux/x64/Dockerfile](./linux/x64/Dockerfile) → `linux/amd64`
- [containers/linux/arm/Dockerfile](./linux/arm/Dockerfile) → `linux/arm64`
- [containers/windows/x64/Dockerfile](./windows/x64/Dockerfile) → `windows/amd64`
- [containers/windows/arm/Dockerfile](./windows/arm/Dockerfile) → `windows/arm64`

All images build the MCP host as **Native AOT**, **self-contained**, **single-file**.

## Build arguments

Each Dockerfile accepts:

- `VERSION` (default `1.0.0`)
- `BUILD_CONFIGURATION` (default `Release`)
- `SOURCE_REPOSITORY` (OCI source label)

## Local builds

```powershell
# Linux amd64
docker build -f containers/linux/x64/Dockerfile -t freenaute-freebox-mcp:linux-amd64 .

# Linux arm64
docker build -f containers/linux/arm/Dockerfile -t freenaute-freebox-mcp:linux-arm64 .

# Windows amd64
docker build -f containers/windows/x64/Dockerfile -t freenaute-freebox-mcp:windows-amd64 .

# Windows arm64
docker build -f containers/windows/arm/Dockerfile -t freenaute-freebox-mcp:windows-arm64 .
```

## Runtime

The MCP transport is stdio. Run containers attached with `-i` (no port publishing required):

```powershell
docker run --rm -i freenaute-freebox-mcp:linux-amd64
```

## CI/CD publishing model

[release.yml](../.github/workflows/release.yml) pushes:

- Linux image tags:
  - `ghcr.io/<owner>/<repo>:<version>-linux-amd64`
  - `ghcr.io/<owner>/<repo>:<version>-linux-arm64`
  - compatibility aliases `:linux-amd64`, `:linux-arm64`
- Linux multi-arch manifests:
  - `ghcr.io/<owner>/<repo>:<version>`
  - `ghcr.io/<owner>/<repo>:latest`
- Windows image tags (manual `workflow_dispatch` only, opt-in inputs):
  - `ghcr.io/<owner>/<repo>:<version>-windows-amd64`
  - `ghcr.io/<owner>/<repo>:<version>-windows-arm64`
  - compatibility aliases `:windows-amd64`, `:windows-arm64`

## Policy validation

No local Docker daemon is required for policy checks:

```powershell
dotnet msbuild -t:ValidateContainers
```

The validation target (in [Directory.Build.targets](../Directory.Build.targets)) enforces:

- all 4 Dockerfiles exist
- MCR base images only
- explicit Native AOT publish settings
- Linux non-root runtime user (`USER 1654`)
- architecture/RID markers per Dockerfile
- no secret-like build arguments or environment values
