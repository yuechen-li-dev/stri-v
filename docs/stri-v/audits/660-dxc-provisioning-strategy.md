# 660 — DXC provisioning strategy (M4e)

## 1) Files changed

- `docs/stri-v/audits/660-dxc-provisioning-strategy.md`
- `docs/stri-v/building-core.md`
- `striv/build/striv-probe-dxc.sh`

## 2) Current state recap

M4d already made DXC compile-smoke testing optional in `StriV.ShaderPipeline.Tests`: tests probe `dxc`, validate help/flags, and skip cleanly when unavailable. In this environment, DXC is currently absent and optional behavior remains green.

## 3) Provisioning options

| Option | Platforms | Trust/provenance | Pros | Cons | Recommendation |
|---|---|---|---|---|---|
| Manual PATH install | Win/Linux/containers | Depends on operator source | Smallest process change, explicit | Inconsistent team setup if undocumented | Recommended only with documented trusted sources |
| Vulkan SDK-provided DXC | Win/Linux | Trusted SDK distributor (LunarG/Khronos packaging) | Common Vulkan workflow, includes HLSL/SPIR-V toolchain | PATH/tool precedence pitfalls; may differ from VS DXC flavor | Recommended as one valid source |
| Official DirectXShaderCompiler GitHub release asset | Win/Linux | Microsoft official repo release artifacts | Clear provenance, version pinning possible, good CI fit | Requires explicit download/cache logic | Recommended primary non-vendored source |
| OS package manager (`apt`) | Linux | Distro packaging | Easy installs when available | No first-class `dxc` package found here; naming mismatch risk | Not primary for now |
| CI explicit download + cache | CI | If pinned to official DXC release URLs | Reproducible and fast after cache warmup | Needs careful script ownership and hash/version pinning | Recommended next CI hardening step |
| Vendored binary in repo | Any | Poor/opaque long-term provenance | Immediate convenience | Violates doctrine; bloats repo; stale/security risk | **Not allowed** |

## 4) Probe results

Host command evidence:

- `command -v dxc || true`
  - exit code: `0`
  - result: no path emitted (`dxc` not found).
- `dxc --help | head -n 40 || true`
  - exit code: `0` (guarded)
  - first meaningful warning/error: `/bin/bash: line 2: dxc: command not found`.
- `apt-cache search dxc | head -n 40 || true`
  - exit code: `0`
  - result: unrelated matches (e.g., `libgridxc-dev`), no obvious DirectX Shader Compiler package.
- `apt-cache search directx | head -n 40 || true`
  - exit code: `0`
  - result: `directx-headers-dev` only in observed output.
- `apt-cache search shader | grep -i dxc | head -n 40 || true`
  - exit code: `0`
  - result: no output.

`-spirv` support observation in this environment: not observable (DXC missing).

Tiny compile attempt: not attempted (DXC missing).

Internet source checks (trusted/official):
- Microsoft DirectXShaderCompiler releases show Linux artifacts containing `dxc`/`libdxcompiler.so` for Ubuntu LTS x86_64.
- Vulkan docs indicate SDK distributions include DXC for HLSL->SPIR-V workflows and note PATH precedence nuances versus Visual Studio-provided DXC.

## 5) Implemented helper

Implemented `striv/build/striv-probe-dxc.sh`.

Behavior:
- default non-strict mode:
  - returns success if DXC is missing,
  - prints detection status and exits early without failure.
- strict mode `--require`:
  - returns nonzero when DXC is missing/unhealthy.
- probes:
  - PATH detection and resolved executable path,
  - `dxc --help` health check,
  - `-spirv` flag detection from help text,
  - optional tiny temp HLSL -> SPIR-V compile-smoke (temp dir only).
- optional `--no-compile-smoke` to skip compile check even if DXC exists.

No installer/downloader script added in M4e: kept smallest safe scope and avoided introducing download/provenance/hash management logic in this step.

## 6) Test validation

### Command results

1) `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj`
- exit code: `0`
- first meaningful warning/error: none
- pass/fail: pass
- output truncated: no

2) `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj --no-build -v minimal`
- exit code: `0`
- first meaningful warning/error: none observed
- pass/fail: pass
- output truncated: no

3) `./striv/build/striv-probe-dxc.sh`
- exit code: `0`
- first meaningful warning/error: none (reported `dxc` not found as informational in non-strict mode)
- pass/fail: pass
- output truncated: no

## 7) Policy decision

- DXC remains optional for default/local tests.
- DXC-dependent compile-smoke should auto-activate when `dxc` is present.
- CI may later opt into strict DXC validation via explicit provisioning + `--require`/DXC-enabled test environment.
- No DXC binaries are committed to source control.
- M4 parser/lowering tests remain independent of DXC availability.

## 8) Recommended next task

Recommended next task: **add CI DXC provisioning** with explicit version pinning and cache, sourcing binaries only from official Microsoft DirectXShaderCompiler release artifacts.

## Sources consulted

- Microsoft official releases: <https://github.com/microsoft/DirectXShaderCompiler/releases>
- Vulkan SDK DXC usage docs: <https://vulkan.lunarg.com/doc/view/latest/windows/DXC.html>
- Vulkan HLSL/DXC guide: <https://vulkan.lunarg.com/doc/view/1.4.304.1/windows/antora/guide/latest/hlsl.html>
