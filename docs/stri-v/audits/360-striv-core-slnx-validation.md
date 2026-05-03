# Stri-V Core `.slnx` Validation Report

## 1) Files changed

- `build/StriV.Core.slnx`
- `docs/stri-v/audits/350-m1-golden-path-summary.md`
- `docs/stri-v/audits/360-striv-core-slnx-validation.md`
- `docs/stri-v/building-core.md`

## 2) `.slnx` design

- **Path/name**: `build/StriV.Core.slnx`.
- **Included projects**:
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
  13. `samples/StriV/CoreSmoke/StriV.CoreSmoke.csproj`
  14. `sources/core/Stride.Core.AssemblyProcessor/Stride.Core.AssemblyProcessor.csproj`
- **Intentionally excluded projects/subsystems**:
  - Shader compiler chain (`Stride.Shaders.Compiler`, CppNet/SDSL source compilation path)
  - `Stride.Audio`
  - `Stride.VirtualReality`
  - old `Stride.Physics`
  - Bepu companion/debug/navigation/soft/2D/tests modules
  - editor/assets/presentation/mobile/tests/extra samples
- **Why AssemblyProcessor is included**: Stri-V Core build flows depend on source-building AssemblyProcessor first, then routing build properties to that output path/hash.
- **Why `Stride.sln` remains**: legacy workflows are explicitly preserved; this task is additive organization, not deletion/cleanup.

## 3) Commands attempted

1. `dotnet sln build/StriV.Core.slnx list`
   - Exit code: `0`
   - First meaningful warning/error: none
   - Classification: **PASS**
   - Output truncated: no

2. `dotnet build sources/core/Stride.Core.AssemblyProcessor/Stride.Core.AssemblyProcessor.csproj -c Debug -f net10.0`
   - Exit code: `0`
   - First meaningful warning/error: warning `CS1030` in `ObjectIdBuilder.cs` (`PERF: Do not copy byte-for-byte.`)
   - Classification: **PASS (with warnings)**
   - Output truncated: yes (terminal capture truncated tail in harness)

3. `AP_BASE="$(realpath sources/core/Stride.Core.AssemblyProcessor/bin/Debug/net10.0)/"; dotnet restore build/StriV.Core.slnx -p:StridePlatforms=Linux -p:StrideGraphicsApis=Vulkan -p:StrideIncludeShaderCompiler=false -p:StrideIncludeAudio=false -p:StrideIncludeVirtualReality=false -p:StrideAssemblyProcessorFramework=net10.0 -p:StrideAssemblyProcessorBasePath="$AP_BASE" -p:StrideAssemblyProcessorHash=sourcebuild`
   - Exit code: `0`
   - First meaningful warning/error: warning `NU1510` (`System.Memory` will not be pruned) from `Stride.Graphics`
   - Classification: **PASS (with warnings)**
   - Output truncated: no

## 4) `.slnx` support observations

- Current `.NET SDK 10.0.107` CLI supports `.slnx` generation/list/restore in this environment.
- `dotnet sln ... list` works directly against `.slnx`.
- `dotnet restore` against `.slnx` also works when Stri-V Core properties are supplied.
- Restore still traverses transitive references from included projects (expected), but the `.slnx` itself remains curated to the intended golden-path entries.

## 5) M1 closeout validation

- M1g and M1h were **not rerun** in this task to keep scope focused on organizational `.slnx` + documentation deliverables.
- Prior validation evidence remains in:
  - `docs/stri-v/audits/300-coresmoke-m1g-validation.md`
  - `docs/stri-v/audits/340-coresmoke-m1h-xvfb-vulkan-validation.md`

## 6) Worktree status

Command:

```bash
git status --short
```

Observed status at report capture time:

```text
 M docs/stri-v/building-core.md
?? build/StriV.Core.slnx
?? docs/stri-v/audits/350-m1-golden-path-summary.md
?? docs/stri-v/audits/360-striv-core-slnx-validation.md
```

## 7) Recommended next task

Because `.slnx` is usable here, recommend:
1. Treat `build/StriV.Core.slnx` as the primary Stri-V Core developer solution.
2. Add a tiny Stri-V-focused CI flow around this curated spine.
3. Next technical track: **shader pipeline prep** (HLSL + Stri-V extension strategy with CppNet quarantine/removal planning).
