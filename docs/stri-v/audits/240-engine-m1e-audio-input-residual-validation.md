# 240 — Engine M1e audio/input residual validation

## 1) Files changed
- `sources/engine/Stride.Engine/Stride.Engine.csproj`
- `sources/engine/Stride.Engine/Engine/ScriptComponent.cs`
- `docs/stri-v/audits/240-engine-m1e-audio-input-residual-validation.md`

## 2) Problem recap
M1e already supports optional shader compiler/audio/VR via `StrideIncludeShaderCompiler=false`, `StrideIncludeAudio=false`, and `StrideIncludeVirtualReality=false` in the M1e scripts. After prior VR residual guards, the current first blocker was in `ScriptComponent.cs`, where script API surface still referenced both audio and input members. For Stri-V Core, audio is intentionally excluded in M1e and must be guarded by `STRIDE_ENGINE_WITHOUT_AUDIO`, while input is validated in M1d and remains part of the core spine.

## 3) `Stride.Engine.csproj` changes
- Added direct project reference: `..\Stride.Input\Stride.Input.csproj`.
- Inclusion is unconditional.
- This is appropriate because `Stride.Engine` directly exposes `InputManager` references in active source, so dependency should be explicit and not transitive/solution-coincidental.
- Existing shader/audio/VR properties and unrelated project references were left unchanged.

## 4) `ScriptComponent.cs` changes
- Guarded `using Stride.Audio;` under `#if !STRIDE_ENGINE_WITHOUT_AUDIO`.
- Guarded `AudioSystem Audio` property and `audioSystem` backing field with the same symbol.
- Left `using Stride.Input;` and `InputManager Input` unguarded.
- No fake audio services/stubs introduced.

## 5) Search verification
Command used:
- `rg -n "using Stride.Audio|AudioSystem|AudioListener|AudioEmitter|SoundBase|SoundInstance|IPlayableSound|IMediaPlayer|InputManager" sources/engine/Stride.Engine`

Results summary:
- Audio hits:
  - `ScriptComponent.cs` audio references are now guarded.
  - Multiple remaining active audio hits exist in `Engine/Game.cs`, `Engine/AudioEmitterComponent.cs`, `Engine/AudioListenerComponent.cs`, and `Engine/Design/EntityCloner.cs`.
  - `Audio/*.cs` hits are excluded by `Compile Remove="Audio\*.cs"` when audio is disabled.
  - Remaining active `Engine/*.cs` audio hits are still problematic under no-audio mode.
- Input hits:
  - Active expected references in `ScriptComponent.cs`, `Game.cs`, and `InputSystem.cs`.
  - Dependency is compile-backed via direct `Stride.Engine -> Stride.Input` project reference.
  - No new input-reference-specific compile blocker observed after this change.

## 6) Validation results
### Command 1
- Command: `./build/striv-build-engine-m1e.sh`
- Exit code: `1`
- First meaningful warning/error: first post-change hard errors are `CS0234`/`CS0246` in active `Stride.Engine/Engine/Audio*.cs` and related files due to missing `Stride.Audio` types under `StrideIncludeAudio=false`.
- Classification: **Fail**
- Output truncated: **Yes** (tool output truncation occurred).

### Optional PowerShell command
- Command not executed: `pwsh ./build/striv-build-engine-m1e.ps1`
- Reason: `pwsh` not available in this environment (`command -v pwsh` returned non-zero).

## 7) Next blocker
First new blocker after this repair:
- Project: `sources/engine/Stride.Engine/Stride.Engine.csproj`
- File(s):
  - `sources/engine/Stride.Engine/Engine/AudioEmitterComponent.cs`
  - `sources/engine/Stride.Engine/Engine/AudioListenerComponent.cs`
  - `sources/engine/Stride.Engine/Engine/Design/EntityCloner.cs`
- Error: `CS0234` / `CS0246` due to remaining active audio references while `StrideIncludeAudio=false` removes `Audio/*.cs` and the audio project reference.
- Category: **unexpected audio residuals**.

## 8) M1e verdict
| Candidate                     | Verdict             | Current blocker                     | Next action |
| ----------------------------- | ------------------- | ----------------------------------- | ----------- |
| `build/StriV.Engine.M1e.slnf` | Adopt after repair  | Active `Engine/*.cs` audio residuals under no-audio mode | Add narrower guards/exclusions for remaining active audio-bound engine files/API surfaces |

## 9) Recommended next task
Given current failure category, recommend: **narrower audio guard repair**.
