# Container images — Freenaute-Freebox-MCP

OCI images for the local **stdio** MCP server (`mcp/Freenaute.Freebox.Mcp`): C# 15 / .NET 11,
**Native AOT**, **single-file**, **self-contained**, chiseled and non-root on Linux.

Two Dockerfiles, one per OS family:

| File | Targets | Built on |
| --- | --- | --- |
| `containers/Dockerfile` | Linux `linux-x64`, `linux-arm64` (multi-arch) | any Linux-capable builder (Linux, or Windows + WSL2/Docker Desktop in Linux mode) |
| `containers/windows/Dockerfile` | Windows containers `win-x64`, `win-arm64` | **Windows hosts only** (CI runners `windows-latest` / `windows-11-arm`) |

All base images come from `mcr.microsoft.com`, pinned by tag (or via a build ARG whose
default is a pinned MCR image). `scratch` is never used.

## Image architecture

Both Dockerfiles are multi-stage:

1. **build** — `mcr.microsoft.com/dotnet/sdk:11.0.100-resolute-aot` (Linux, ships the
   clang/lld Native AOT toolchain) or `mcr.microsoft.com/dotnet/sdk:11.0.100-nanoserver-ltsc2025`
   (Windows). `COPY . .` uses the repo-root `.dockerignore` to keep the context small, then
   `dotnet restore` + `dotnet publish -c Release -r <RID> --self-contained
   -p:PublishAot=true -p:PublishSingleFile=true -p:InvariantGlobalization=true
   -p:Version=<VERSION> -o <out>`.
2. **final** — Linux: `mcr.microsoft.com/dotnet/runtime-deps:11.0-resolute-chiseled`
   (chiseled = no shell, no package manager, non-root **UID 1654**), only the AOT binary is
   copied to `/app`, `USER 1654`, `ENTRYPOINT ["/app/Freenaute.Freebox.Mcp"]`. Windows:
   `mcr.microsoft.com/windows/nanoserver:ltsc2025` (or `...:ltsc2025-arm64`), binary at
   `C:\app\Freenaute.Freebox.Mcp.exe`, `ENTRYPOINT ["C:\\app\\Freenaute.Freebox.Mcp.exe"]`.

### Why `resolute-chiseled` and not `alpine-chiseled`

Checked against `https://mcr.microsoft.com/v2/dotnet/runtime-deps/tags/list` on **2026-11-21**:
no `alpine-chiseled` tag exists (only `alpine` / `alpine-extra`). The supported perimeter is
glibc `linux-x64` / `linux-arm64`, so the glibc-based `11.0-resolute-chiseled` is kept: it is the
lightest MCR base that still ships glibc + CA certificates (needed by the AOT binary) while
remaining shell-free and non-root.

### RID selection (TARGETARCH mapping)

With BuildKit/buildx, `ARG TARGETARCH` is mapped automatically: `amd64 → linux-x64` /
`win-x64`, `arm64 → linux-arm64` / `win-arm64`; any other value fails the build. An explicit
`--build-arg RID=...` is still honored when `TARGETARCH` is empty (legacy builder), and the RID
whitelist is enforced in both Dockerfiles (`linux-x64|linux-arm64`, `win-x64|win-arm64`).

### Verified MCR tags (checked 2026-11-21 via `/v2/<repo>/tags/list` and manifests)

| Image | Tag | Architectures |
| --- | --- | --- |
| `dotnet/sdk` | `11.0.100-resolute-aot` | linux/amd64, linux/arm64 |
| `dotnet/runtime-deps` | `11.0-resolute-chiseled` | linux/amd64, linux/arm/v7, linux/arm64 |
| `dotnet/sdk` | `11.0.100-nanoserver-ltsc2025` | windows/amd64 only |
| `windows/nanoserver` | `ltsc2025` | windows/amd64 |
| `windows/nanoserver` | `ltsc2025-arm64` | windows/arm64 |

Note: no `arm64` Windows tag exists in `dotnet/sdk` (the .NET SDK Windows images are amd64-only),
so win-arm64 builds use the amd64 SDK image under x64 emulation (Windows 11 ARM 24H2+) or
cross-publish `win-arm64` from an amd64 Windows runner, with only the final stage on
`mcr.microsoft.com/windows/nanoserver:ltsc2025-arm64`.

## Build

Linux, single arch (default x64):

```powershell
docker build -f containers/Dockerfile -t freenaute-freebox-mcp:local .
```

Linux, multi-arch manifest list (one build, both platforms — preferred):

```powershell
docker buildx build --platform linux/amd64,linux/arm64 `
  -f containers/Dockerfile `
  -t ghcr.io/freenaute/freenaute-freebox-mcp:0.1.0-beta `
  -t ghcr.io/freenaute/freenaute-freebox-mcp:latest --push .
```

Equivalent per-platform builds with an explicit RID (push separately):

```powershell
docker buildx build --platform linux/amd64 -f containers/Dockerfile -t <reg>/freenaute-freebox-mcp:0.1.0-beta-amd64 --push .
docker buildx build --platform linux/arm64 -f containers/Dockerfile -t <reg>/freenaute-freebox-mcp:0.1.0-beta-arm64 --push .
docker buildx imagetools create -t <reg>/freenaute-freebox-mcp:0.1.0-beta <reg>/freenaute-freebox-mcp:0.1.0-beta-amd64 <reg>/freenaute-freebox-mcp:0.1.0-beta-arm64
```

Windows (run on a Windows host/runner with Windows containers enabled):

```powershell
# win-x64
docker build -f containers/windows/Dockerfile -t freenaute-freebox-mcp:win-x64 .
# win-arm64 (final base ltsc2025-arm64; pass RID explicitly)
docker build -f containers/windows/Dockerfile `
  --build-arg BASE_IMAGE=mcr.microsoft.com/windows/nanoserver:ltsc2025-arm64 `
  --build-arg RID=win-arm64 `
  -t freenaute-freebox-mcp:win-arm64 .
```

## Run (stdio MCP — `-i`, never `-t`)

The server speaks JSON-RPC over **stdin/stdout**; all logging/diagnostics go to **stderr**, so
stdout stays reserved for the MCP protocol. Do not allocate a TTY and do not publish a port:

```powershell
docker run -i --rm freenaute-freebox-mcp:local
```

Typical MCP client config:

```json
{
  "mcpServers": {
    "freebox": {
      "command": "docker",
      "args": ["run", "-i", "--rm", "ghcr.io/freenaute/freenaute-freebox-mcp:latest"]
    }
  }
}
```

## Configuration

Non-secret settings are passed as ordinary runtime environment variables by the MCP client:

```powershell
docker run -i --rm `
  -e FREEBOX_APPLICATION_ID=io.github.freenaute.freebox-mcp `
  -e FREEBOX_CREDENTIAL_PROFILE=default `
  freenaute-freebox-mcp:local
```

## Secrets policy — nothing secret ever enters the image

- No secret is baked into a Dockerfile, build arg, label, or the build context: there is no
  `ENV`/`ARG` whose name or value mentions tokens, secrets, passwords or credentials, and
  `.dockerignore` excludes `.env*`, `*.pem`, `*.key`, `*.pfx`, `*.p12`.
- The Freebox token is **never** passed to the container. On Linux the credential backend is
  **fail-closed by default** (`UnsupportedCredentialStorage`), so pairing/import operations are
  unavailable inside the Linux image unless an encrypted-file store is mounted explicitly.
- Optional encrypted-file store (when enabled in the app): mount the files read-only, e.g.
  under `/run/secrets/`, and point the app at them with **runtime** env vars only:

  ```powershell
  docker run -i --rm `
    -v ${PWD}/secrets:/run/secrets:ro `
    -e FREEBOX_CREDENTIAL_FILE=/run/secrets/freebox-credentials.enc `
    -e FREEBOX_CREDENTIAL_PASSPHRASE_FILE=/run/secrets/freebox-passphrase `
    freenaute-freebox-mcp:local
  ```

  (With Docker secrets: `--secret freebox_credentials --secret freebox_passphrase` mounts them
  under `/run/secrets/`.) The Dockerfiles never set these variables; only the runtime
  invocation does.
- On Windows containers the app uses the Windows Credential Manager (DPAPI), which is isolated
  per container.

## Platform matrix and macOS

| Platform | Image | Notes |
| --- | --- | --- |
| linux/amd64 | `containers/Dockerfile` | buildx `--platform linux/amd64` |
| linux/arm64 | `containers/Dockerfile` | buildx `--platform linux/arm64` |
| windows/amd64 | `containers/windows/Dockerfile` | build on `windows-latest` |
| windows/arm64 | `containers/windows/Dockerfile` + `--build-arg BASE_IMAGE=...:ltsc2025-arm64 --build-arg RID=win-arm64` | build on `windows-11-arm` |
| **macOS** | **none — deliberately out of scope** | Docker Desktop on macOS only runs a Linux VM; there is no macOS kernel container image/runtime. Use the native single-file binaries (`dotnet publish -r osx-x64` / `osx-arm64`, see the root README) on macOS instead. |

Windows container images can only be built/run on Windows hosts; Linux images cannot be built
from a Windows host unless Docker is switched to Linux containers (WSL2).

## Build context (`.dockerignore`)

The root `.dockerignore` keeps the context minimal and secret-free: `.git`, `.github`, `.idea`,
`.vs`, `.vscode`, `.artifacts`, `**/bin`, `**/obj`, `**/TestResults`, `*.binlog`, `*.user`,
`.env*`, `*.pem`, `*.key`, `*.pfx`, `*.p12`, plus `eng/`, `tests/` and `containers/`.
`eng/` (CI/release scripts) and `tests/` are safe to exclude because the container build only
restores/publishes `mcp/Freenaute.Freebox.Mcp/Freenaute.Freebox.Mcp.csproj` — neither the root
`Directory.Build.props`/`.targets` nor that project reference them — and `containers/` avoids
recursion when building from the repo root. Everything the build needs (`global.json`,
`NuGet.Config`, `Directory.Build.props`, `Directory.Build.targets`, `Directory.Packages.props`,
`libs/`, `mcp/`) stays in the context.

## Static validation

No Docker daemon is required for policy checks: the MSBuild `ValidateContainers` target
statically enforces the rules above (MCR-only pinned bases, `USER 1654` + explicit ENTRYPOINT +
`-p:PublishAot=true` + RID whitelist on Linux, Nano Server LTSC 2025 + `win-x64|win-arm64` on
Windows, OCI labels, and the no-secrets-in-`ENV`/`ARG` rule) over `containers/**`.
