# M1e Engine Slice Validation (`Stride.Engine` admission)

## 1) Files changed

- `build/StriV.Engine.M1e.slnf`
- `build/striv-build-engine-m1e.sh`
- `build/striv-build-engine-m1e.ps1`
- `docs/stri-v/building-core.md`
- `docs/stri-v/audits/170-engine-m1e-validation.md`

## 2) Solution filter contents

- Base solution: `Stride.sln`
- Initial explicit projects included (11):
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
- Explicitly excluded from the filter as requested: Bepu, old physics, editor/assets/presentation/mobile/tests/samples/packaging slices.
- Transitive explicit additions after initial attempt: none. Restore/build proceeded without requiring additional explicit `.slnf` project entries.

## 3) Script design

- Scripts mirror M1d bootstrap scripts (same structure and validation flow) with M1e names/paths.
- Repo root detection:
  - Bash: resolves script directory with `BASH_SOURCE[0]`, then `..`.
  - PowerShell: resolves from `$MyInvocation.MyCommand.Path`, then `..`.
- Configuration handling:
  - Bash positional arg defaults `Debug`, accepts `Debug|Release`, forwards remaining args.
  - PowerShell `-Configuration Debug|Release` defaults `Debug`, forwards remaining args.
- Linux/Vulkan property routing: both scripts pass `StridePlatforms=Linux` and `StrideGraphicsApis=Vulkan`.
- AssemblyProcessor build/validation:
  - Build `sources/core/Stride.Core.AssemblyProcessor/Stride.Core.AssemblyProcessor.csproj` first.
  - Validate DLL exists at `.../bin/<Configuration>/net10.0/`.
  - Validate size > 1024 bytes.
  - Validate not Git LFS pointer text.
  - Validate PE header starts with `MZ`.
- M1e build invocation/properties:
  - Build `build/StriV.Engine.M1e.slnf`.
  - Pass:
    - `StridePlatforms=Linux`
    - `StrideGraphicsApis=Vulkan`
    - `StrideAssemblyProcessorFramework=net10.0`
    - `StrideAssemblyProcessorBasePath=<absolute AP output dir with trailing slash>`
    - `StrideAssemblyProcessorHash=sourcebuild`

## 4) Validation results

### Command 1
- Command: `./build/striv-build-engine-m1e.sh`
- Exit code: `1`
- First meaningful warning/error:
  - First engine-admission blocker category: shader/compiler dependency compile failure in `Stride.Core.Shaders`:
    - `error CS0246: The type or namespace name 'CppNet' could not be found`
- Pass/fail: **Fail**
- Output truncated: **Yes** (tool output was truncated due volume)

### Command 2
- Command: `./build/striv-build-engine-m1e.sh Release`
- Not run (per instruction: stop after Debug failure)

### Command 3
- Command: `pwsh ./build/striv-build-engine-m1e.ps1`
- Not run (PowerShell not available in this Linux environment)

## 5) Engine/transitive dependency observations

- `Stride.Engine` restore/build status: restore progressed and compile started through transitive graph, but overall M1e build failed.
- Transitive projects observed during restore/build include:
  - `Stride.Audio`
  - `Stride.Rendering`
  - `Stride.VirtualReality`
  - `Stride.Shaders.Compiler`
- None of the above had to be explicitly added to `.slnf`.
- `Stride.VirtualReality` did not present the first blocker in this run.
- Shader compiler/parser/toolchain area did cause the first blocker (`CppNet` types missing in `Stride.Core.Shaders`).
- Audio/native also failed later in same run (`lld`/`libCelt.a` pointer-like content), but this was not first.
- Rendering compiled far enough to produce warnings; not first blocker.
- Asset/compiler/editor leakage signals observed transitively (e.g., assets/package project restores), but no explicit asset/editor projects were added to filter.
- First meaningful engine-related error recorded:
  - `/sources/shaders/Stride.Core.Shaders/Parser/PreProcessor.cs` missing `CppNet` namespace/types.

## 6) VR handling recommendation

- VR was not the first blocker in this run, so no VR exclusion action is proposed yet.
- Keep VR unchanged for this task; prioritize shader/toolchain blocker isolation first.

## 7) M1e verdict

| Candidate                     | Verdict               | Current blocker                                                | Next action |
| ----------------------------- | --------------------- | -------------------------------------------------------------- | ----------- |
| `build/StriV.Engine.M1e.slnf` | Adopt after repair    | Shader/compiler dependency compile blocker (`CppNet` missing). | Run shader compiler isolation audit and minimal condition plan. |

## 8) Worktree status

Command run:

```bash
git status --short
```

Observed status after this task:

```text
 M docs/stri-v/building-core.md
?? build/StriV.Engine.M1e.slnf
?? build/striv-build-engine-m1e.ps1
?? build/striv-build-engine-m1e.sh
?? docs/stri-v/audits/170-engine-m1e-validation.md
```

## 9) Recommended next task

Because M1e failed first in shader compiler/parser/toolchain area:
- **Recommend shader compiler isolation audit** for `Stride.Core.Shaders`/`Stride.Shaders.Compiler` (`CppNet` dependency resolution path and minimal Stri-V conditioning strategy).
