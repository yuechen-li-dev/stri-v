# 250 - Engine M1e audio residual guard validation

## 1) Files changed
- `sources/engine/Stride.Engine/Stride.Engine.csproj`
- `sources/engine/Stride.Engine/Engine/Design/EntityCloner.cs`
- `docs/stri-v/building-core.md`
- `docs/stri-v/audits/250-engine-m1e-audio-residual-guard-validation.md`

## 2) Audio residual problem recap
- In no-audio M1e (`StrideIncludeAudio=false`), `Stride.Audio` project reference is already conditioned out.
- `Engine/Audio/*.cs` and prior `ScriptComponent.Audio` no-audio handling were already in place.
- Remaining active audio references were specifically in:
  - `Engine/AudioEmitterComponent.cs`
  - `Engine/AudioListenerComponent.cs`
  - `Engine/Design/EntityCloner.cs`

## 3) `Stride.Engine.csproj` changes
- Added narrow no-audio compile removals under `StrideIncludeAudio=false` for:
  - `Engine\AudioEmitterComponent.cs`
  - `Engine\AudioListenerComponent.cs`
- No additional non-requested broad wildcards were introduced.
- Existing behavior remains unchanged:
  - `StrideIncludeAudio` default-on property remains.
  - conditioned `Stride.Audio` project reference remains.
  - `STRIDE_ENGINE_WITHOUT_AUDIO` compile symbol remains.

## 4) `EntityCloner.cs` changes
- Guarded the audio-specific clone serializer registration for `Sound` with:
  - `#if !STRIDE_ENGINE_WITHOUT_AUDIO` / `#endif`
- Removed unconditional `using Stride.Audio;` from the file.
- Non-audio clone behavior (entity/component tree collection, clone context, serialization/deserialization flow) was not changed.

## 5) Search verification
Search root: `sources/engine/Stride.Engine`

Terms searched:
- `using Stride.Audio`
- `AudioSystem`
- `AudioListener`
- `AudioEmitter`
- `AudioEmitterComponent`
- `AudioListenerComponent`
- `SoundBase`
- `SoundInstance`
- `IPlayableSound`
- `IMediaPlayer`

Classification of results:
- Guarded by `#if !STRIDE_ENGINE_WITHOUT_AUDIO`:
  - `Engine/Game.cs`
  - `Engine/ScriptComponent.cs`
  - `Engine/Design/EntityCloner.cs` (`Sound` serializer attribute)
- Excluded by compile removal in no-audio mode:
  - `Audio/*.cs` via existing `Compile Remove="Audio\*.cs"`
  - `Engine/AudioEmitterComponent.cs` (new)
  - `Engine/AudioListenerComponent.cs` (new)
- Comments/docs-only hits:
  - XML docs and comments inside excluded audio files.
- Still problematic active hits after this repair attempt:
  - None from the targeted audio residual set above.

## 6) Validation results
### Command 1
- Command: `./build/striv-build-engine-m1e.sh`
- Exit code: `1`
- Pass/fail: **Fail**
- Output truncated: **Yes** (tool output truncated)
- First meaningful error (first new blocker):
  - `/workspace/stri-v/sources/engine/Stride.Engine/Shaders.Compiler/EffectCompilerFactory.cs(29,32): error CS0246: The type or namespace name 'EffectCompiler' could not be found`

### Command 2
- Command: `./build/striv-build-engine-m1e.sh Release`
- Not executed (per instruction: Debug failed).

### Optional PowerShell command
- Command: `pwsh ./build/striv-build-engine-m1e.ps1`
- Not executed (`pwsh` not available in environment).

## 7) Next blocker
- First new blocker:
  - Project: `sources/engine/Stride.Engine/Stride.Engine.csproj`
  - File: `sources/engine/Stride.Engine/Shaders.Compiler/EffectCompilerFactory.cs`
  - Error: `CS0246` (`EffectCompiler` missing)
- Category: **shader residuals** (shader compiler boundary leakage in no-shader-compiler M1e path).

## 8) M1e verdict

| Candidate                     | Verdict            | Current blocker                                                                                       | Next action |
| ----------------------------- | ------------------ | ----------------------------------------------------------------------------------------------------- | ----------- |
| `build/StriV.Engine.M1e.slnf` | Adopt after repair | `Stride.Engine/Shaders.Compiler/EffectCompilerFactory.cs` -> `CS0246` missing `EffectCompiler` type | Run shader residual audit and narrowly guard/exclude remaining shader-compiler-bound sources |

## 9) Recommended next task
- Since M1e currently fails due to shader residuals, recommended next task is: **shader residual audit**.
