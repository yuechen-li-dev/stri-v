# M1c Platform + Graphics Basics Validation (130)

## 1) Files changed

- `build/StriV.PlatformGraphicsBasics.M1c.slnf`
- `build/striv-build-platform-graphics-basics-m1c.sh`
- `build/striv-build-platform-graphics-basics-m1c.ps1`
- `docs/stri-v/building-core.md`

## 2) Solution filter contents

- Base solution: `Stride.sln`
- Explicit included projects:
  1. `sources/core/Stride.Core/Stride.Core.csproj`
  2. `sources/core/Stride.Core.Mathematics/Stride.Core.Mathematics.csproj`
  3. `sources/core/Stride.Core.IO/Stride.Core.IO.csproj`
  4. `sources/core/Stride.Core.MicroThreading/Stride.Core.MicroThreading.csproj`
  5. `sources/core/Stride.Core.Serialization/Stride.Core.Serialization.csproj`
  6. `sources/core/Stride.Core.Reflection/Stride.Core.Reflection.csproj`
  7. `sources/engine/Stride/Stride.csproj`
  8. `sources/engine/Stride.Games/Stride.Games.csproj`
  9. `sources/engine/Stride.Graphics/Stride.Graphics.csproj`
- Confirmed excluded from explicit filter: `Stride.Input`, `Stride.Engine`, rendering/audio/VR/editor/assets/presentation/mobile/sample/test projects.
- Transitive explicit additions required after validation: **none**.

## 3) Script design

- Both scripts mirror M1a/M1b bootstrap flow:
  1. resolve repo root from script directory,
  2. build source AssemblyProcessor,
  3. validate produced AP payload,
  4. build the M1c solution filter.
- Repo root detection:
  - Bash: `SCRIPT_DIR` via `BASH_SOURCE`, then `REPO_ROOT` via `..`.
  - PowerShell: `$ScriptDir` via `$MyInvocation.MyCommand.Path`, then `Resolve-Path` parent.
- Configuration handling:
  - Bash positional argument (`Debug` default, `Release` accepted) + extra args forwarding.
  - PowerShell `-Configuration Debug|Release` + remaining args forwarding.
- Linux/Vulkan routing:
  - `StridePlatforms=Linux`
  - `StrideGraphicsApis=Vulkan`
- AssemblyProcessor routing:
  - `StrideAssemblyProcessorFramework=net10.0`
  - `StrideAssemblyProcessorBasePath=<absolute net10.0 AP output dir + trailing slash>`
  - `StrideAssemblyProcessorHash=sourcebuild`
- AP validation checks:
  - expected output path exists,
  - dll size > 1024 bytes,
  - first text bytes not Git LFS pointer,
  - first two bytes are `MZ`.

## 4) Validation results

1. Command: `./build/striv-build-platform-graphics-basics-m1c.sh`
   - Exit code: `0`
   - First meaningful warning/error: warning-only output; first notable platform-related warning during restore was `NU1510` on `Stride.Graphics` package pruning.
   - Classification: **PASS**
   - Output truncated: **yes** (terminal capture truncated due output volume).

2. Command: `./build/striv-build-platform-graphics-basics-m1c.sh Release`
   - Exit code: `0`
   - First meaningful warning/error: warning-only output; similarly included `NU1510` and existing code-analysis/compiler warnings.
   - Classification: **PASS**
   - Output truncated: **yes** (terminal capture truncated due output volume).

3. Command: `pwsh ./build/striv-build-platform-graphics-basics-m1c.ps1`
   - Not executed: `pwsh` unavailable in this Linux environment (`command not found`).

## 5) Platform/graphics observations

- `Stride.Games` and `Stride.Graphics` both restored and built successfully in Debug and Release.
- `net10.0-windows` was not required for this slice validation path.
- No SDL symbol / `STRIDE_UI_SDL` blocker occurred in this run.
- No Vulkan native dependency blocker occurred in this run.
- No unexpected D3D package-condition failure blocker occurred in this run.
- Shader-related transitive projects were restored transitively (`Stride.Shaders`), but no explicit shader parser/compiler projects were added to the `.slnf`.
- No freetype/native payload blocker occurred in this run.
- First meaningful platform/graphics warning seen: `Stride.Graphics.csproj` `NU1510` package prune warning (non-blocking).

## 6) M1c verdict

| Candidate                                     | Verdict | Current blocker | Next action |
| --------------------------------------------- | ------- | --------------- | ----------- |
| `build/StriV.PlatformGraphicsBasics.M1c.slnf` | Adopt   | None in this validation run | Proceed to M1d-prep input slice audit |

## 7) Worktree status

Command:

```bash
git status --short
```

Observed status after reverting AP hash noise:

```text
 M docs/stri-v/building-core.md
?? build/StriV.PlatformGraphicsBasics.M1c.slnf
?? build/striv-build-platform-graphics-basics-m1c.ps1
?? build/striv-build-platform-graphics-basics-m1c.sh
?? docs/stri-v/audits/130-platform-graphics-basics-m1c-validation.md
```

## 8) Recommended next task

M1c build succeeded in Debug and Release, so the recommended next task is: **M1d-prep for input slice**.
