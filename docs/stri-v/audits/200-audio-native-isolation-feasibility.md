# M1e-fix2-prep: Audio/native isolation feasibility (static audit)

Date: 2026-05-03 (UTC)
Scope: static analysis only; no builds/tests/package installs/project patches.

## 1) Evidence collection

### Commands executed
- `rg -n "using Stride.Audio|Stride.Audio|AudioSystem|AudioListener|AudioEmitter|AudioEmitterComponent|AudioListenerComponent|Sound|SoundInstance|SoundMusic|SoundEffect|MediaSound|AudioEngine|AudioDevice|OpenAL|Celt|libCelt|Opus|opus|strideaudio|Stride.Native|NativePath|DllImport|LibraryImport|Native" sources/engine/Stride.Engine sources/engine/Stride.Audio sources/native deps build docs`
- `rg -n "<ProjectReference|<PackageReference|TargetFramework|TargetFrameworks|StrideRuntime|StrideAssemblyProcessor|StrideAssemblyProcessorOptions|StrideGraphicsApiDependent|UseWPF|UseWindowsForms|Audio|Native|OpenAL|Celt|Opus|DllImport|LibraryImport" sources/engine/Stride.Audio sources/engine/Stride.Engine sources/native sources/Directory.Packages.props`
- `find sources/engine/Stride.Audio -maxdepth 4 -type f | sort`
- `find sources/native -maxdepth 4 -type f | sort`
- `nl -ba sources/engine/Stride.Engine/Stride.Engine.csproj | sed -n '1,140p'`
- `nl -ba sources/engine/Stride.Audio/Stride.Audio.csproj | sed -n '1,220p'`
- `nl -ba sources/native/Stride.Native.targets | sed -n '1,260p'`
- `nl -ba sources/engine/Stride.Audio/Stride.Native.Libs.targets | sed -n '1,220p'`
- `nl -ba sources/engine/Stride.Engine/Engine/Game.cs | sed -n '1,470p'`
- `python` probe of `deps/NativePath/dotnet/linux-x64/libCelt.a` bytes/size
- `nl -ba deps/NativePath/dotnet/linux-x64/libCelt.a | sed -n '1,40p'`
- `apt-cache search libcelt | head -n 20`
- `apt-cache search libopus | head -n 20`
- `apt-cache search openal | head -n 20`

### Search terms used
`Stride.Audio`, `AudioSystem`, `AudioListener`, `AudioEmitter`, `Sound`, `SoundInstance`, `OpenAL`, `Celt`, `libCelt`, `Opus`, `Stride.Native`, `NativePath`, `DllImport`, `LibraryImport`.

### Files directly read
- `sources/engine/Stride.Engine/Stride.Engine.csproj`
- `sources/engine/Stride.Engine/Engine/Game.cs`
- `sources/engine/Stride.Engine/Audio/*`
- `sources/engine/Stride.Audio/Stride.Audio.csproj`
- `sources/engine/Stride.Audio/Stride.Native.Libs.targets`
- `sources/engine/Stride.Audio/Native/Celt.cpp`
- `sources/engine/Stride.Audio/Native/Celt.cs`
- `sources/engine/Stride.Audio/Native/OpenAL.cpp`
- `sources/native/Stride.Native.targets`
- `deps/NativePath/dotnet/linux-x64/libCelt.a`

### Scripts/tools used
- Inline Python snippet for binary/text fingerprinting of `libCelt.a`.

### Non-modification note
- This prep did not patch project/source/build scripts.
- Only this report file was created.

### Static-only uncertainty
- Compile-time break list is inferred from references/usages; exact compiler diagnostics require a later build run.
- Runtime behavior for particular scene/content combinations is inferred from code paths, not executed.

## 2) M1e audio blocker recap

- Shader compiler opt-out was already introduced (`StrideIncludeShaderCompiler=false` path in `Stride.Engine.csproj`).
- After that, first blocker moved to audio native link path (`Stride.Audio` -> `Stride.Native.targets` -> linux native link).
- Reported failing artifact path: `deps/NativePath/dotnet/linux-x64/libCelt.a` with `lld` parse failure (`unknown directive: version`).
- Therefore this audit focuses on audio/native isolation (not VR/rendering): this is the current first blocker after shader-compiler isolation.

## 3) `Stride.Engine` dependency on Audio

### Direct project dependency
- `Stride.Engine.csproj` has an unconditional project reference to `..\Stride.Audio\Stride.Audio.csproj`.
- It already demonstrates a pattern for optionality (`StrideIncludeShaderCompiler` + conditional project reference + compile symbol), which is a plausible template for audio.

### Audio usage inside `Stride.Engine`
Observed direct usage areas:
- `Engine/Game.cs`
  - `using Stride.Audio;`
  - property `public AudioSystem Audio { get; }`
  - constructor creates/registers `AudioSystem` + `IAudioEngineProvider`
  - `Initialize()` adds audio system to `GameSystems`.
- `Engine/AudioListenerComponent.cs`
- `Audio/AudioSystem.cs`
- `Audio/AudioListenerProcessor.cs`
- `Audio/AudioEmitterProcessor.cs`
- `Audio/AudioEmitterSoundController.cs`

Observed types used include:
- `AudioSystem`, `IAudioEngineProvider`, `AudioEngine`, `AudioDevice`
- `AudioListener`, `AudioEmitter`
- `SoundBase`, `SoundInstance`, `IPlayableSound`, `IMediaPlayer`
- entity components/processors for listeners/emitters.

### Usage classification
- **runtime-critical for current engine wiring**: `Game` service registration and system insertion for audio.
- **optional subsystem registration candidate**: the `Game` constructor/initialize audio service/system steps are central places to gate.
- **component definitions / scene-entity audio components**: listener/emitter components/processors/controllers in engine tree.
- **asset/content integration**: sound-related controller paths reference sound instance APIs.
- **unknown**: downstream projects/scenes that assume audio services always present.

### If `Stride.Audio` reference is removed without guards
Likely compile failures (inference from static references): missing type/namespace errors in `Game.cs` and `sources/engine/Stride.Engine/Audio/*.cs`, plus any internal assembly-friend/test references tied to audio.

### Plausibility of `StrideIncludeAudio=false`
- **Yes, plausible** by symmetry with shader compiler opt-out:
  - conditional `ProjectReference` in `Stride.Engine.csproj`
  - compile symbol to exclude/alternate code paths in `Game` and audio component/processor sources.
- Breadth is non-trivial but localized (audio-specific files + `Game.cs` service wiring).

## 4) `Stride.Audio` project audit

### Project-level characteristics
- `Stride.Audio.csproj` sets `StrideNativeOutputName=libstrideaudio` and marks runtime/AP processing enabled.
- Direct project refs: `Stride.Native`, `Stride`.
- Native source files listed: `Native/Celt.cpp`, `Native/OpenAL.cpp`, `Native/OpenSLES.cpp`, `Native/XAudio2.cpp`.

### Native target involvement
- `sources/native/Stride.Native.targets` auto-collects `*.c/*.cpp`, produces per-runtime native outputs, and on Linux links with `lld` including `@(StrideNativePathLibsLinux)` and `libNativePath.a`.
- `sources/engine/Stride.Audio/Stride.Native.Libs.targets` contributes `StrideNativePathLibsLinux Include="libCelt.a"`.

### Framework/runtime/windows desktop coupling
- No direct WPF/WinForms flags in this project file.
- Contains cross-platform native branches and Windows/UWP-specific branches in shared native targets (not editor desktop UI coupling, but native toolchain coupling).

### Linux/native assumptions
- Linux link path expects `deps/NativePath/dotnet/linux-x64/libCelt.a` and `libNativePath.a` artifacts.
- OpenAL is dynamically loaded (`libopenal.so.1` attempt present in native code), so runtime availability matters even after successful build.

### Separable optional module?
- Statics suggest **yes**: `Stride.Audio` is a coherent module with explicit project boundary and engine-facing touchpoints that can be guarded.

### Recommendation
- For M1e Core admission: **defer audio module** (exclude conditionally), keep default-on for upstream behavior, and mark loud TODO for dedicated audio-native follow-up.

## 5) Native audio dependency audit

### What is `libCelt.a` in this checkout?
- Direct byte/text inspection shows a 3-line Git LFS pointer text file:
  - `version https://git-lfs.github.com/spec/v1`
  - `oid sha256:...`
  - `size 222408`
- Actual file size on disk is ~131 bytes, not a static archive of declared payload size.

### Archive validity
- Not a valid `ar` static library in current checkout (text pointer, no archive magic).

### What links against it?
- Linux native link command in `Stride.Native.targets` includes `@(StrideNativePathLibsLinux->.../%(Filename).a)`.
- `Stride.Audio/Stride.Native.Libs.targets` defines `StrideNativePathLibsLinux` as `libCelt.a`, so `Stride.Audio` native link consumes it.

### Is `libstrideaudio` being built?
- Yes by configuration intent: `StrideNativeOutputName=libstrideaudio` in `Stride.Audio.csproj`, linked into runtime native output.

### Is OpenAL involved?
- Yes. `OpenAL.cpp` dynamically loads OpenAL symbols; Linux fallback includes `libopenal.so.1`.

### Celt central or optional in this stack?
- For current Stride audio implementation, Celt/Opus-custom bridge appears embedded in sound decode/encode path (`Celt.cs`, `Celt.cpp`, compressed sound sources). So “optional” only with broader code-path gating or alternate decoder strategy.

### Opus plausibility (later)
- Static evidence shows use of `opus_custom_*` APIs via `deps/Celt/include/opus_custom.h`; this is an Opus custom-mode lineage path, but replacing prebuilt Celt artifact strategy with system libs requires ABI/symbol/audit work.

## 6) Possible repair paths

### Path A — fetch/fix native payload
- Required artifact: real `deps/NativePath/dotnet/linux-x64/libCelt.a` (likely via LFS hydration or equivalent artifact source).
- Preserves old architecture: yes.
- Risk: **medium** (artifact provenance, reproducibility, cross-platform consistency).
- Codex/PR compatibility: possible but violates current prep doctrine and doesn’t advance Core isolation strategy.

### Path B — system Opus/OpenAL
- Plausible eventually, but static evidence indicates Stride expects current symbol/packaging flow (`libCelt.a`, custom wrappers, NativePath layout).
- Likely needs target edits, include/lib path changes, potential C++ wrapper adjustments, runtime packaging changes.
- Risk: **high** without dedicated native audio ABI audit; should be deferred.

### Path C — condition out `Stride.Audio` from `Stride.Engine` (Core)
- Later change: conditionalize `Stride.Engine` -> `Stride.Audio` project reference.
- Add compile guards in direct usage sites (`Game.cs` and engine audio component/processor files).
- Lost behavior: no in-engine audio services/components/playback for Core runtime.
- Scope: narrow, reversible, aligned with staged Stri-V Core extraction.

### Path D — architectural split later
- Moving audio components fully out of `Stride.Engine` into `Stride.Audio` (or dedicated module) is plausible long-term but larger than M1e repair.

## 7) Recommended M1e repair plan (smallest credible)

Recommended next implementation (not done in this prep):
1. Add `StrideIncludeAudio` defaulting to `true` in `Stride.Engine.csproj`.
2. Condition audio project reference on `'$(StrideIncludeAudio)' != 'false'`.
3. Add compile symbol for no-audio mode (e.g., `STRIDE_ENGINE_WITHOUT_AUDIO`) when disabled.
4. Guard direct audio usages in `Game.cs` and audio-tied engine files.
5. Update M1e build script invocation to pass `-p:StrideIncludeAudio=false` (only for Stri-V Core slice path).
6. Add bold TODO doc note: audio/native stack excluded for M1e Core; `libCelt.a` payload and broader audio-native strategy deferred.

If guarded surface becomes unexpectedly broad during implementation, stop at first compile blocker and report exact file/type.

## 8) No-audio runtime limitation

In `StrideIncludeAudio=false` Core mode:
- No `AudioSystem` service registration.
- No listener/emitter processing and no sound playback pipeline.
- Scenes/assets relying on audio components/controllers will be non-functional (or require compatibility stubs/conditional content handling).

## 9) Ubuntu package availability note (static environment evidence)

From local package index queries (`apt-cache search`):
- No `libcelt` package appeared.
- `libopus-dev`, `libopus0`, and `libopenal-dev`/`libopenal1` are available.

Interpretation: modern Ubuntu ecosystem appears Opus/OpenAL-oriented; legacy Celt package availability is not evident here.

## 10) Risk register

| Risk | Area | Likelihood | Impact | Evidence | Mitigation |
| ---- | ---- | ---------: | -----: | -------- | ---------- |
| Audio types are widespread in `Stride.Engine`. | Managed compile | High | Medium | `Game.cs` + multiple `Stride.Engine/Audio/*` files reference audio types. | Gate by property + targeted compile guards. |
| Excluding audio causes compile errors in registration/component code. | Managed compile | High | Medium | Unconditional refs/usages today. | Guard `Game` wiring and audio files first; iterate on first blocker only. |
| Runtime scenes with audio components won’t work in M1e Core no-audio mode. | Runtime behavior | High | Medium | Audio system/components are removed/guarded. | Document loud limitation/TODO; keep default-on outside Core slice. |
| `libCelt.a` is LFS pointer/stub in checkout. | Native link | High | High | File contents are Git LFS pointer text, tiny size. | Avoid native audio in M1e; defer payload fix. |
| Fixing native audio preserves old architecture and defers modularization. | Architecture | Medium | Medium | Current stack tightly couples legacy native payloads. | Prioritize Core isolation now, revisit audio module later. |
| Replacing Celt with Opus requires ABI/API audit. | Native/audio | High | High | Current code uses `opus_custom_*` wrappers and specific link layout. | Dedicated follow-up audit before any replacement. |
| VR/rendering may become next blockers after audio exclusion. | Slice progress | Medium | Medium/High | `Stride.Engine` still references VR/rendering modules. | Accept staged blocker discovery; report first new blocker precisely. |
| Audio should become its own module slice later. | Roadmap | Medium | Medium | Clear project boundary exists but engine has direct couplings. | Plan future refactor once M1e admitted. |

## 11) Recommended implementation prompt (for next Codex task)

```md
Implement only M1e audio opt-out repair for Stri-V Core.

Scope:
- Add `StrideIncludeAudio` default-on property in `sources/engine/Stride.Engine/Stride.Engine.csproj`.
- Condition `Stride.Engine` -> `Stride.Audio` `ProjectReference` on `'$(StrideIncludeAudio)' != 'false'`.
- When audio is disabled, define a symbol (e.g. `STRIDE_ENGINE_WITHOUT_AUDIO`) and add minimal compile guards in `Stride.Engine` files that directly reference audio types (start with `Engine/Game.cs` and `sources/engine/Stride.Engine/Audio/*`).
- Update only the M1e build script path to pass `-p:StrideIncludeAudio=false` for Stri-V Core validation.
- Add a bold TODO in Stri-V docs stating that audio/native stack is intentionally excluded for M1e Core and `libCelt.a`/native audio requires dedicated future audit.

Validation constraints:
- Run only: `build/striv-build-engine-m1e.sh`.
- Do not fix `libCelt.a`.
- Do not attempt VR/rendering/native-audio remediation if they become next blockers.
- Stop at the first new blocker and report exact project/file/error lines.
- Avoid broad audio rewrites/refactors.
```
