# 260 — Engine M1e shader residual guard validation

## 1) Files changed
- `sources/engine/Stride.Engine/Stride.Engine.csproj`
- `docs/stri-v/building-core.md`
- `docs/stri-v/audits/260-engine-m1e-shader-residual-guard-validation.md`

## 2) Shader residual problem recap
- In no-shader-compiler M1e, the external `Stride.Shaders.Compiler` project reference was already conditionally excluded via `StrideIncludeShaderCompiler=false`.
- The prior CppNet blocker was already removed from the current blocker chain.
- Remaining active source was engine-local shader compiler integration under `sources/engine/Stride.Engine/Shaders.Compiler/`.
- This task adds compile-time exclusion of that engine-local residual integration source in no-shader-compiler mode.

## 3) `Stride.Engine.csproj` changes
- Added a new conditional compile removal item group under `Condition="'$(StrideIncludeShaderCompiler)' == 'false'"`:
  - `Compile Remove="Shaders.Compiler\*.cs"`
  - `Compile Remove="Shaders.Compiler\Internals\*.cs"`
- Confirmed unchanged behavior:
  - `StrideIncludeShaderCompiler` remains default-on (`true` when unset).
  - Conditioned `ProjectReference` to `..\Stride.Shaders.Compiler\Stride.Shaders.Compiler.csproj` remains unchanged.
  - `STRIDE_ENGINE_WITHOUT_SHADER_COMPILER` compile symbol behavior remains unchanged.
- No unrelated project references were changed.

## 4) Engine-local shader compiler folder audit
Files found under `sources/engine/Stride.Engine/Shaders.Compiler/`:
- `EffectCompilerFactory.cs`
- `RemoteEffectCompiler.cs`
- `RemoteEffectCompilerClient.cs`
- `RemoteEffectCompilerEffectAnswer.cs`
- `RemoteEffectCompilerEffectRequest.cs`
- `RemoteEffectCompilerEffectRequested.cs`
- `Internals/NetworkVirtualFileProvider.cs`

Assessment:
- Folder is compiler-integration-only (effect compiler creation, remote compile client messaging, related request/answer contracts, and supporting network virtual file provider).
- All files in the folder are now excluded in no-shader-compiler M1e mode via csproj compile-item removal.

## 5) Search verification
Search scope: `sources/engine/Stride.Engine`

Terms checked:
- `using Stride.Shaders.Compiler`
- `EffectCompiler`
- `EffectCompilerFactory`
- `RemoteEffectCompiler`
- `NullEffectCompiler`
- `CompilerResults`
- `CompilerParameters`
- `ShaderCompiler`
- `StrideIncludeShaderCompiler`
- `STRIDE_ENGINE_WITHOUT_SHADER_COMPILER`

Classification:
- **Excluded by compile removal**
  - All hits within `sources/engine/Stride.Engine/Shaders.Compiler/*.cs`
  - `sources/engine/Stride.Engine/Shaders.Compiler/Internals/*.cs`
- **Guarded by `#if !STRIDE_ENGINE_WITHOUT_SHADER_COMPILER`**
  - `sources/engine/Stride.Engine/Engine/Game.cs` compiler wiring (`using` + `EffectCompilerFactory.CreateEffectCompiler(...)`)
- **Comments/docs**
  - `sources/engine/Stride.Engine/Engine/Design/EffectCompilationMode.cs` XML docs referencing compiler types/modes.
  - `sources/engine/Stride.Engine/Stride.Engine.csproj` property/reference/symbol lines.
- **Still problematic**
  - None found for M1e no-shader-compiler compilation after this change.

## 6) Documentation update
Updated `docs/stri-v/building-core.md` to clarify that no-shader-compiler M1e excludes both:
- external `Stride.Shaders.Compiler` project reference, and
- engine-local `Stride.Engine/Shaders.Compiler` integration source files.

CppNet/SDSL TODO posture remains unchanged.

## 7) Validation results
1. Command: `./build/striv-build-engine-m1e.sh`
   - Exit code: `0`
   - First meaningful warning/error: warning(s) only (e.g., NU1901 advisory warnings in package restore).
   - Classification: **PASS**
   - Output truncated: **Yes** (tool output truncation occurred).

2. Command: `./build/striv-build-engine-m1e.sh Release`
   - Exit code: `0`
   - First meaningful warning/error: warning(s) only (e.g., NU1510/NU1901 warnings).
   - Classification: **PASS**
   - Output truncated: **Yes** (tool output truncation occurred).

3. Command: `pwsh ./build/striv-build-engine-m1e.ps1` (optional)
   - Exit code: `0` from wrapper check.
   - First meaningful warning/error: `pwsh not available`.
   - Classification: **NOT EXECUTED (environment tool unavailable)**
   - Output truncated: **No**.

## 8) Next blocker
- No new blocker encountered in this slice.
- M1e build completed in both Debug and Release with warnings only.

## 9) M1e verdict

| Candidate                     | Verdict | Current blocker | Next action |
| ----------------------------- | ------- | --------------- | ----------- |
| `build/StriV.Engine.M1e.slnf` | Adopt   | None            | Move to M1f-prep scope |

## 10) Recommended next task
- Since M1e builds, recommend **M1f-prep for adding `Stride.BepuPhysics`**.
