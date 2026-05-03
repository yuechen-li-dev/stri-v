# 280 - Engine Bepu M1f validation

## 1) Files changed

- `build/StriV.Engine.Bepu.M1f.slnf`
- `build/striv-build-engine-bepu-m1f.sh`
- `build/striv-build-engine-bepu-m1f.ps1`
- `docs/stri-v/building-core.md`
- `docs/stri-v/audits/280-engine-bepu-m1f-validation.md`

## 2) Solution filter contents

- Base solution: `Stride.sln`
- Explicit included projects (12):
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
  11. `sources/engine/Stride.Engine/Stride.Engine.csproj`
  12. `sources/engine/Stride.BepuPhysics/Stride.BepuPhysics/Stride.BepuPhysics.csproj`
- Confirmed excluded systems (not explicitly admitted in M1f filter):
  - `sources/engine/Stride.Physics*`
  - `Stride.BepuPhysics.Debug`
  - `Stride.BepuPhysics.Navigation`
  - `Stride.BepuPhysics.Soft`
  - `Stride.BepuPhysics._2D`
  - `Stride.BepuPhysics.Tests`
  - samples/tests/editor/assets/presentation/mobile slices as explicit admissions
- Transitive additions after validation: **none**. Initial filter validated without adding extra explicit projects.

## 3) Script design

Both scripts mirror the existing M1e bootstrap flow:

- Repo root detection from script directory (`build/..`).
- Configuration handling:
  - Linux script positional `Debug|Release` (default `Debug`) and extra args forwarding.
  - PowerShell `-Configuration Debug|Release` and remaining args forwarding.
- Linux/Vulkan routing is fixed in both:
  - `StridePlatforms=Linux`
  - `StrideGraphicsApis=Vulkan`
- Shader/audio/VR opt-outs are always passed:
  - `StrideIncludeShaderCompiler=false`
  - `StrideIncludeAudio=false`
  - `StrideIncludeVirtualReality=false`
- AssemblyProcessor source-build + payload validation:
  - Build `sources/core/Stride.Core.AssemblyProcessor/Stride.Core.AssemblyProcessor.csproj`
  - Validate output directory `.../bin/<Configuration>/net10.0/`
  - Require DLL exists
  - Require size > 1024 bytes
  - Detect and reject Git LFS pointer prefix
  - Require `MZ` header bytes
- M1f build invocation targets `build/StriV.Engine.Bepu.M1f.slnf` with:
  - `StrideAssemblyProcessorFramework=net10.0`
  - `StrideAssemblyProcessorBasePath=<absolute AP output dir with trailing slash>`
  - `StrideAssemblyProcessorHash=sourcebuild`

## 4) Validation results

### Command 1
- Command: `./build/striv-build-engine-bepu-m1f.sh`
- Exit code: `0`
- Classification: **PASS**
- First meaningful warning/error: warnings only; no compile errors.
- Output truncated: **yes** in captured terminal transcript (tool token cap), but command completed and returned success.

### Command 2
- Command: `./build/striv-build-engine-bepu-m1f.sh Release`
- Exit code: `0`
- Classification: **PASS**
- First meaningful warning/error: warnings only; no compile errors.
- Output truncated: **yes** in captured terminal transcript (tool token cap), but command completed and returned success.

### Command 3 (optional PowerShell)
- Command: `pwsh ./build/striv-build-engine-bepu-m1f.ps1`
- Exit code: `127`
- Classification: **NOT EXECUTED IN ENVIRONMENT**
- First meaningful warning/error: `/bin/bash: line 1: pwsh: command not found`
- Output truncated: **no**

## 5) Bepu observations

- `Stride.BepuPhysics` restore/build: **succeeded** in Debug and Release runs (with warnings, no errors).
- Old `Stride.Physics` pulled: **not observed** as an explicit admitted project or as a first-class compile blocker.
- Bepu companion modules pulled: **not observed** as explicit admitted projects or compile blockers.
- Bepu package restore issues: no restore failure; warning `NU5104` reports stable package depending on prerelease `BepuPhysics [2.5.0-beta.28, )`.
- Rendering/gizmo compile blockers: not blockers; `CollidableGizmo` warnings present but compile succeeded.
- Native dependency issues: no first-failure native blocker observed in this slice validation.
- First meaningful Bepu-related warning: `NU5104` prerelease dependency warning for `BepuPhysics` package range.

## 6) M1f verdict

| Candidate                          | Verdict | Current blocker | Next action |
| ---------------------------------- | ------- | --------------- | ----------- |
| `build/StriV.Engine.Bepu.M1f.slnf` | Adopt   | None            | Move to M1g-prep for next minimal runtime module or first executable/sample slice. |

## 7) Worktree status

Command run:

```bash
git status --short
```

Status after M1f work:

- `M docs/stri-v/building-core.md`
- `?? build/StriV.Engine.Bepu.M1f.slnf`
- `?? build/striv-build-engine-bepu-m1f.ps1`
- `?? build/striv-build-engine-bepu-m1f.sh`
- `?? docs/stri-v/audits/280-engine-bepu-m1f-validation.md`

## 8) Recommended next task

Because M1f builds in both Debug and Release, recommend:

- **M1g-prep for the next runtime module or first minimal executable/sample slice.**
