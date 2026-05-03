# M1e shader compiler opt-out validation

## 1) Files changed
- `sources/engine/Stride.Engine/Stride.Engine.csproj`
- `sources/engine/Stride.Engine/Engine/Game.cs`
- `build/striv-build-engine-m1e.sh`
- `build/striv-build-engine-m1e.ps1`
- `docs/stri-v/building-core.md`
- `docs/stri-v/audits/190-engine-m1e-shader-compiler-optout-validation.md`

## 2) Property design
- Property name: `StrideIncludeShaderCompiler`.
- Default value: `true` when unset via:
  - `<StrideIncludeShaderCompiler Condition="'$(StrideIncludeShaderCompiler)' == ''">true</StrideIncludeShaderCompiler>`
- Legacy behavior preservation:
  - Existing Stride behavior is preserved because shader compiler project reference remains active unless explicit opt-out sets the property to `false`.
- Stri-V M1e opt-out path:
  - M1e scripts pass `-p:StrideIncludeShaderCompiler=false`.
- Why narrow:
  - This only gates shader compiler integration for `Stride.Engine` and does not multiplex unrelated Stri-V core concerns under a broad `StriVCore=true` switch.

## 3) `Stride.Engine.csproj` changes
- Added default `StrideIncludeShaderCompiler=true` when unset.
- Conditioned project reference:
  - `..\Stride.Shaders.Compiler\Stride.Shaders.Compiler.csproj`
  - included only when `'$(StrideIncludeShaderCompiler)' != 'false'`.
- Added conditional define when excluded:
  - `STRIDE_ENGINE_WITHOUT_SHADER_COMPILER` when `'$(StrideIncludeShaderCompiler)' == 'false'`.
- No other project references were changed.

## 4) `Game.cs` changes
- Guarded `using Stride.Shaders.Compiler;` with `#if !STRIDE_ENGINE_WITHOUT_SHADER_COMPILER`.
- Guarded `EffectCompilerFactory.CreateEffectCompiler(...)` assignment with the same define.
- When shader compiler is excluded:
  - `EffectSystem` is still created and registered.
  - `EffectSystem.Compiler` is left unset by `Game` initialization path (no fallback assignment introduced).
- `NullEffectCompiler` is not referenced.
- Known runtime limitation:
  - This is compile-slice isolation only; runtime behavior requiring source shader compilation through `Stride.Shaders.Compiler` is not validated/resolved here.

## 5) Build script changes
- Added `-p:StrideIncludeShaderCompiler=false` in:
  - `build/striv-build-engine-m1e.sh`
  - `build/striv-build-engine-m1e.ps1`
- Confirmed existing properties remain:
  - `StridePlatforms=Linux`
  - `StrideGraphicsApis=Vulkan`
  - `StrideAssemblyProcessorFramework=net10.0`
  - `StrideAssemblyProcessorBasePath=<AP output dir>`
  - `StrideAssemblyProcessorHash=sourcebuild`

## 6) Validation results
### Command 1
- Command: `./build/striv-build-engine-m1e.sh`
- Exit code: `1`
- First meaningful warning/error in M1e stage: warning
  - `Stride.Engine.csproj : warning NU1510 ... System.Threading.Tasks.Dataflow will not be pruned`
- First meaningful **error** (build blocker):
  - `lld : error : .../deps/NativePath/dotnet/linux-x64/libCelt.a:1: unknown directive: version`
  - followed by `MSB3073` from `Stride.Native.targets` while linking `Stride.Audio` native output.
- Classification: **Fail** (new non-shader blocker)
- Output truncated: **Yes** (tool output reported truncation).

### Command 2
- Command: `./build/striv-build-engine-m1e.sh Release`
- Not run.
- Reason: per instruction, stopped after first new blocker in Debug.

### Command 3 (optional)
- Command: `pwsh ./build/striv-build-engine-m1e.ps1`
- Not run.
- Reason: per instruction, stopped after first new blocker in Debug.

## 7) Shader compiler isolation observations
- `Stride.Shaders.Compiler` was **not** restored/built in the observed M1e Debug run.
- The prior `Stride.Core.Shaders/Parser/PreProcessor.cs` `CppNet` compile error did not reappear.
- No new `CppNet` or `Stride.Core.Shaders` parser compile failure was observed in this run.
- M1e compile path proceeded into other engine modules and failed later in audio native linkage.
- No additional compiler-type errors attributable to the shader compiler opt-out branch were observed before the new blocker.

## 8) Next blocker
- First new blocker: native audio link failure.
- Project/file/error:
  - Project: `sources/engine/Stride.Audio/Stride.Audio.csproj`
  - Invoked target path: `sources/native/Stride.Native.targets`
  - Error: `lld` cannot consume `deps/NativePath/dotnet/linux-x64/libCelt.a` (appears pointer/stub content, `unknown directive: version`), then `MSB3073`.
- Category: **audio/native deps**.

## 9) M1e verdict

| Candidate                     | Verdict             | Current blocker                  | Next action |
| ----------------------------- | ------------------- | -------------------------------- | ----------- |
| `build/StriV.Engine.M1e.slnf` | Adopt after repair | Audio native link (`libCelt.a`) | Audio dependency isolation or conditioning |

## 10) CppNet TODO confirmation
Confirmed `docs/stri-v/building-core.md` now includes a TODO section that states:
- CppNet is legacy shader tooling.
- It is not part of Stri-V Core.
- It should eventually be removed/replaced/quarantined.
- No CppNet removal was performed in this task.

## 11) Recommended next task
Because M1e currently fails due to audio/native dependencies, the recommended next task is:
- **audio dependency isolation or conditioning**.
