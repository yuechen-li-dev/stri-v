# 500 — Clean graph rendering admission validation

## 1) Files changed
- `striv/projects/Stride.Rendering/Stride.Rendering.csproj` (new)
- `striv/projects/Stride.Engine/Stride.Engine.csproj`
- `striv/StriV.Core.slnx`

## 2) Blocker recap
- Failing project prior to this repair: `striv/projects/Stride.Engine/Stride.Engine.csproj`.
- Representative missing symbols were `SkeletonUpdater`, `RenderGroup`, `RenderContext`, and `RenderModel` from engine-side files (notably `ModelComponent.cs`).
- Those symbols are rendering runtime/model assembly symbols (namespace `Stride.Rendering`) and indicate the clean graph lacked the runtime rendering assembly reference.

## 3) Rendering project audit
- Old project path: `sources/engine/Stride.Rendering/Stride.Rendering.csproj`.
- Source/type definitions for missing symbols:
  - `sources/engine/Stride.Rendering/Rendering/SkeletonUpdater.cs`
  - `sources/engine/Stride.Rendering/Rendering/RenderGroup.cs`
  - `sources/engine/Stride.Rendering/Rendering/RenderContext.cs`
  - `sources/engine/Stride.Rendering/Rendering/RenderModel.cs`
- Old `Stride.Engine` directly references `Stride.Rendering`: yes, via `ProjectReference` in `sources/engine/Stride.Engine/Stride.Engine.csproj`.
- Old `Stride.Rendering` direct project refs: only `../Stride.Games/Stride.Games.csproj`.
- Old `Stride.Rendering` package refs: none in project file.
- Old `Stride.Rendering` AP options: `StrideAssemblyProcessor=true`; no explicit options override in that project file.
- Shader compiler references in rendering source:
  - Rendering runtime files reference `Stride.Shaders.Compiler` types (`EffectSystem`, `RootEffectRenderFeature`, etc.).
  - This is source-level namespace/type usage, not a `Stride.Shaders.Compiler` project dependency in the old rendering csproj.
- Asset/editor/native dependencies:
  - Old rendering project sets `StridePackAssets=true` (legacy SDK behavior), but has no direct editor/presentation/mobile project references.

## 4) Clean rendering project changes
- New project: `striv/projects/Stride.Rendering/Stride.Rendering.csproj`.
- Source globs:
  - `../../../sources/engine/Stride.Rendering/**/*.cs`
  - linked `../../../sources/shared/SharedAssemblyInfo.cs`
- Source excludes:
  - only standard `**/bin/**;**/obj/**` exclusion (no additional source exclusions were needed in this repair).
- Project refs:
  - `../Stride.Games/Stride.Games.csproj`
- Package refs:
  - none.
- Define constants:
  - inherited from `striv/build/StriV.Core.Profile.props` (includes `STRIDE_ENGINE_WITHOUT_SHADER_COMPILER`, audio/VR-off defines, Vulkan/Linux desktop defines).
- AP decision:
  - enabled StriV assembly processing by importing `../../build/StriV.AssemblyProcessor.targets` and setting `StriVAssemblyProcessorOptions` to `--parameter-key --auto-module-initializer --serialization`.
- Why shader compiler/parser remains excluded:
  - no `ProjectReference` to `Stride.Shaders.Compiler` or parser/CppNet projects was added;
  - repair is limited to runtime rendering assembly admission and engine linkage.

## 5) Engine project changes
- Added clean `ProjectReference` from `striv/projects/Stride.Engine/Stride.Engine.csproj` to `../Stride.Rendering/Stride.Rendering.csproj`.
- Existing clean exclusions were preserved (shader compiler integration disabled, audio disabled, VR disabled).
- No audio/VR/shader-compiler references were re-enabled.

## 6) Validation results
1. Command: `./striv/build/striv-build-core.sh`
   - Exit code: `0`
   - First meaningful warning/error: NU1510 prune warnings (`System.Memory` in `Stride.Graphics`, `System.Threading.Tasks.Dataflow` in `Stride.Engine`)
   - Pass/fail: **pass**
   - Output truncated: **yes** (long build log)

2. Command: `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj`
   - Exit code: `0`
   - First meaningful warning/error: same NU1510 prune warnings during restore/build phase
   - Pass/fail: **pass** (3 passed, 0 failed)
   - Output truncated: **no**

## 7) First new blocker
- No new blocker encountered in this pass; clean core build completed successfully.

## 8) Test results
- Command: `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj`
- Result: **pass** (3/3 tests passed).
- What this proves:
  - clean graph resolves/builds through admitted rendering + engine path under current profile;
  - clean graph test suite expectations currently pass.
- What this does not prove:
  - runtime behavior/perf correctness of all rendering paths;
  - coverage for untested platform/profile permutations.

## 9) Worktree status
`git status --short` output after changes:
- `M striv/StriV.Core.slnx`
- `M striv/projects/Stride.Engine/Stride.Engine.csproj`
- `?? striv/projects/Stride.Rendering/`
- `?? docs/stri-v/audits/500-clean-graph-rendering-admission-validation.md`

## 10) Recommended next task
- **clean graph runtime smoke**: run a focused smoke path (e.g., `StriV.CoreSmoke`) to validate runtime startup/render-path wiring beyond compile/test graph checks, while keeping shader compiler/audio/VR/editor exclusions intact.
