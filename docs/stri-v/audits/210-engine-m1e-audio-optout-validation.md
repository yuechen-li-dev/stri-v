# 210 — Engine M1e audio opt-out validation

## 1. Files changed
- `sources/engine/Stride.Engine/Stride.Engine.csproj`
- `sources/engine/Stride.Engine/Engine/Game.cs`
- `build/striv-build-engine-m1e.sh`
- `build/striv-build-engine-m1e.ps1`
- `docs/stri-v/building-core.md`
- `docs/stri-v/audits/210-engine-m1e-audio-optout-validation.md`

## 2. Property design
- **Property name:** `StrideIncludeAudio`
- **Default value:** `true` when unset (`Condition="'$(StrideIncludeAudio)' == ''"`).
- **Why default preserves legacy behavior:** default-on keeps the original `Stride.Audio` project reference and existing audio-integrated `Stride.Engine` behavior unless opt-out is explicitly requested.
- **How Stri-V M1e opts out:** M1e scripts now pass `-p:StrideIncludeAudio=false`.
- **Why this is narrower than `StriVCore=true`:** scope is limited to one concern (audio inclusion for `Stride.Engine`) instead of broad mode switches that can accidentally change unrelated project graph behavior.

## 3. `Stride.Engine.csproj` changes
- Added default property:
  - `<StrideIncludeAudio Condition="'$(StrideIncludeAudio)' == ''">true</StrideIncludeAudio>`
- Conditioned project reference:
  - `..\Stride.Audio\Stride.Audio.csproj` now only included when `$(StrideIncludeAudio) != false`.
- Added compile symbol for opt-out:
  - `STRIDE_ENGINE_WITHOUT_AUDIO` when `StrideIncludeAudio=false`.
- Added compile removal gate under opt-out:
  - `<Compile Remove="Audio\*.cs" />` so `Stride.Engine/Audio/*` files are excluded from engine compile in no-audio mode.
- No other project references were changed.

## 4. `Game.cs` changes
- Guarded `using Stride.Audio;` with `#if !STRIDE_ENGINE_WITHOUT_AUDIO`.
- Guarded `Audio` property declaration with `#if !STRIDE_ENGINE_WITHOUT_AUDIO`.
- Guarded constructor audio registration block:
  - `AudioSystem` creation
  - `Services.AddService(Audio)`
  - `Services.AddService<IAudioEngineProvider>(Audio)`
- Guarded `GameSystems.Add(Audio)` in `Initialize()`.
- Behavior when audio is excluded: engine compiles without registering audio services/systems; no fake/stub audio services were introduced.
- Known runtime limitation: no engine audio services/system in this mode.

## 5. `Stride.Engine/Audio/*` changes
- No direct edits to files under `sources/engine/Stride.Engine/Audio/*`.
- Instead, files are fully excluded from `Stride.Engine` compilation when `StrideIncludeAudio=false` using csproj item removal.
- Non-audio engine code affected only by minimal compile guards in `Game.cs`.

## 6. Build script changes
- Added `-p:StrideIncludeAudio=false` to:
  - `build/striv-build-engine-m1e.sh`
  - `build/striv-build-engine-m1e.ps1`
- Confirmed existing properties remain:
  - `StrideIncludeShaderCompiler=false`
  - `StridePlatforms=Linux`
  - `StrideGraphicsApis=Vulkan`
  - `StrideAssemblyProcessorFramework=net10.0`
  - `StrideAssemblyProcessorBasePath=<source-built AP output dir>`
  - `StrideAssemblyProcessorHash=sourcebuild`

## 7. Validation results
1) Command: `./build/striv-build-engine-m1e.sh`
- Exit code: `1`
- First meaningful warning/error: `lld : error : .../deps/NativePath/dotnet/linux-x64/libNativePath.a:1: unknown directive: version`
- Classification: **Fail** (new blocker)
- Output truncated: **Yes** (terminal capture truncated due volume)

2) Command: `./build/striv-build-engine-m1e.sh Release`
- Not run (per instruction: do not run Release when Debug fails).

3) Command: `pwsh ./build/striv-build-engine-m1e.ps1`
- Not run (optional; skipped after Debug failure triage).

## 8. Audio isolation observations
- `Stride.Audio` was **not** restored/built in the Debug run output.
- Prior `libCelt.a` linker error is no longer the first blocker in this M1e path.
- No-audio path removed engine-side direct audio usage via compile guards and compile-item exclusion.
- Default-on behavior appears preserved by project conditions (`StrideIncludeAudio` defaults to `true`).

## 9. Next blocker
- First new blocker:
  - Project: `sources/engine/Stride.VirtualReality/Stride.VirtualReality.csproj`
  - File/target: `sources/native/Stride.Native.targets` invoking `lld`
  - Error: `libNativePath.a:1: unknown directive: version`
- Category: **VR** (VirtualReality native linkage path).

## 10. M1e verdict

| Candidate                     | Verdict             | Current blocker                                                   | Next action |
| ----------------------------- | ------------------- | ----------------------------------------------------------------- | ----------- |
| `build/StriV.Engine.M1e.slnf` | Adopt after repair  | VR native link (`Stride.VirtualReality` -> `libNativePath.a`)    | Run a VR exclusion/conditioning task for M1e |

## 11. Audio TODO confirmation
Confirmed docs now include bold TODO content stating:
- Audio/native stack is intentionally excluded from Stri-V Core M1e.
- `libCelt.a`/NativePath/OpenAL/Opus strategy requires a dedicated future audit.
- No audio-native repair was performed in this task.

## 12. Recommended next task
Because M1e currently fails due to VR native linkage, recommend: **VR exclusion/conditioning task**.
