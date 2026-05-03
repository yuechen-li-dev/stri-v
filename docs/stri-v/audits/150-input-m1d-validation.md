# M1d Input Slice Validation (Linux-first)

## 1) Files changed
- `build/StriV.Input.M1d.slnf`
- `build/striv-build-input-m1d.sh`
- `build/striv-build-input-m1d.ps1`
- `docs/stri-v/building-core.md`
- `docs/stri-v/audits/150-input-m1d-validation.md`

## 2) Solution filter contents
- Base solution: `Stride.sln`.
- Explicit included projects (exactly 10):
  1. `sources/core/Stride.Core/Stride.Core.csproj`
  2. `sources/core/Stride.Core.Mathematics/Stride.Core.Mathematics.csproj`
  3. `sources/core/Stride.Core.IO/Stride.Core.IO.csproj`
  4. `sources/core/Stride.Core.MicroThreading/Stride.Core.MicroThreading.csproj`
  5. `sources/core/Stride.Core.Serialization/Stride.Core.Serialization.csproj`
  6. `sources/core/Stride.Core.Reflection/Stride.Core.Reflection.csproj`
  7. `sources/engine/Stride/Stride.csproj`
  8. `sources/engine/Stride.Games/Stride.Games.csproj`
  9. `sources/engine/Stride.Graphics/Stride.Graphics.csproj`
  10. `sources/engine/Stride.Input/Stride.Input.csproj`
- Confirmed explicitly excluded from filter: `Stride.Engine`, rendering/audio/VR/shader compiler stacks, editor/assets/presentation/mobile/test/sample projects.
- Transitive additions after validation: **none required** in `.slnf`; restore/build resolved transitives without adding explicit projects.

## 3) Script design
- Both scripts mirror the existing M1c bootstrap pattern and naming/logging conventions.
- Repo root detection:
  - Bash: `SCRIPT_DIR` from `${BASH_SOURCE[0]}` then `REPO_ROOT` = parent.
  - PowerShell: `$ScriptDir` from `$MyInvocation.MyCommand.Path`, then `$RepoRoot` = parent.
- Configuration handling:
  - Bash positional argument with default `Debug` and validation to `Debug|Release`; forwards remaining args.
  - PowerShell `-Configuration` with `ValidateSet("Debug","Release")`; forwards remaining args.
- Linux/Vulkan routing properties passed to M1d build:
  - `StridePlatforms=Linux`
  - `StrideGraphicsApis=Vulkan`
  - `StrideAssemblyProcessorFramework=net10.0`
  - `StrideAssemblyProcessorBasePath=<absolute AP bin path with trailing slash>`
  - `StrideAssemblyProcessorHash=sourcebuild`
- AssemblyProcessor bootstrap/validation:
  - Builds `sources/core/Stride.Core.AssemblyProcessor/Stride.Core.AssemblyProcessor.csproj` first.
  - Validates output exists in `bin/<Configuration>/net10.0/`.
  - Validates size > 1024 bytes.
  - Validates not Git LFS pointer prefix.
  - Validates first two bytes = `MZ`.
- Final invocation builds `build/StriV.Input.M1d.slnf` with the above properties and exits nonzero on failures.

## 4) Validation results
| Command | Exit code | First meaningful warning/error | Pass/Fail | Output truncated |
|---|---:|---|---|---|
| `./build/striv-build-input-m1d.sh` | 0 | Warnings only (existing nullable/analyzer/NU warnings); no errors. | Pass | Yes (terminal capture truncated very long build output). |
| `./build/striv-build-input-m1d.sh Release` | 0 | Warnings only (same class of pre-existing warnings); no errors. | Pass | Yes (terminal capture truncated very long build output). |
| `command -v pwsh >/dev/null && pwsh ./build/striv-build-input-m1d.ps1 || echo 'pwsh-not-available'` | 0 | `pwsh-not-available` in this Linux environment. | Not executed (optional) | No |

## 5) Input/platform observations
- `Stride.Input` restored and built successfully in both Debug and Release runs.
- `net10.0-windows` was avoided in the executed Linux-first bootstrap path.
- No unexpected hard build failure from Windows-only input package activation under Linux.
- SDL input path compiled; no blocking `STRIDE_UI_SDL` symbol/path errors observed.
- No WinForms/UWP/mobile source-file compile blockers observed.
- No blocking SDL/gamepad/native dependency restore/build errors observed in this compile-only slice.
- First input-related warnings observed were analyzer warnings in `Stride.Input/SDL/InputSourceSDL.cs` about undisposed fields (CA2213), non-blocking.

## 6) M1d verdict

| Candidate                    | Verdict | Current blocker | Next action |
| ---------------------------- | ------- | --------------- | ----------- |
| `build/StriV.Input.M1d.slnf` | Adopt | None blocking compile validation | Proceed to M1e-prep for next runtime module (likely BepuPhysics or Audio). |

## 7) Worktree status
`git status --short` after changes:

```text
 M docs/stri-v/building-core.md
?? build/StriV.Input.M1d.slnf
?? build/striv-build-input-m1d.ps1
?? build/striv-build-input-m1d.sh
?? docs/stri-v/audits/150-input-m1d-validation.md
```

## 8) Recommended next task
Since M1d builds in Debug and Release, recommend **M1e-prep** for the next runtime module slice, likely **BepuPhysics or Audio**.
