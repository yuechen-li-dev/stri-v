# M1d Prep Audit: `Stride.Input` Slice Feasibility (Static-Only)

Date: 2026-05-03  
Scope: static feasibility audit for adding `sources/engine/Stride.Input/Stride.Input.csproj` above validated M1c slice, Linux-first.

## 1) Evidence collection

### Commands used

```bash
find /workspace -name AGENTS.md -print
find sources/engine/Stride.Input -maxdepth 4 -type f | sort
rg -n "<ProjectReference|<PackageReference|TargetFramework|TargetFrameworks|StrideRuntime|StrideExplicitWindowsRuntime|StrideAssemblyProcessor|StrideAssemblyProcessorOptions|StrideGraphicsApiDependent|UseWPF|UseWindowsForms|Microsoft.WindowsDesktop.App|StridePlatform|StridePlatforms|StrideGraphicsApis|SDL|Sdl|STRIDE_UI_|STRIDE_PLATFORM_|Keyboard|Mouse|GamePad|Gamepad|Touch|Pointer|Gesture|Android|iOS|UWP|Windows|Win32|RawInput|DirectInput|XInput|SharpDX|Silk|OpenTK|VirtualReality|VR|Linux|Vulkan|Direct3D" sources/engine/Stride.Input sources/engine/Stride.Games sources/engine/Stride.Graphics sources/sdk sources/targets
nl -ba sources/engine/Stride.Input/Stride.Input.csproj
nl -ba sources/engine/Stride.Input/InputSourceFactory.cs
nl -ba sources/engine/Stride.Games/Stride.Games.csproj
rg -n "STRIDE_UI_SDL|STRIDE_UI_WINFORMS|STRIDE_UI_WPF|STRIDE_PLATFORM_|StridePlatforms|StrideGraphicsApis|StrideExplicitWindowsRuntime|StrideRuntime|UseWPF|UseWindowsForms" sources/sdk sources/targets sources/engine/Stride*/build
nl -ba sources/sdk/Stride.Build.Sdk/Sdk/Stride.Platform.props
nl -ba sources/sdk/Stride.Build.Sdk/Sdk/Stride.Frameworks.targets
nl -ba sources/sdk/Stride.Build.Sdk/Sdk/Stride.Platform.targets
nl -ba sources/sdk/Stride.Build.Sdk/Sdk/Stride.Graphics.targets
nl -ba sources/engine/Stride.Input/SDL/InputSourceSDL.cs | sed -n '1,220p'
nl -ba sources/engine/Stride.Input/Windows/InputSourceWindowsDirectInput.cs | sed -n '1,180p'
nl -ba sources/engine/Stride.Input/Windows/InputSourceWinforms.cs | sed -n '1,140p'
nl -ba sources/engine/Stride.Input/UWP/InputSourceUWP.cs | sed -n '1,120p'
nl -ba sources/engine/Stride.Input/Android/InputSourceAndroid.cs | sed -n '1,100p'
nl -ba sources/engine/Stride.Input/iOS/InputSourceiOS.cs | sed -n '1,100p'
git status --short
```

### Files opened/read
- `sources/engine/Stride.Input/Stride.Input.csproj`
- `sources/engine/Stride.Input/InputSourceFactory.cs`
- `sources/engine/Stride.Input/SDL/InputSourceSDL.cs`
- `sources/engine/Stride.Input/Windows/InputSourceWindowsDirectInput.cs`
- `sources/engine/Stride.Input/Windows/InputSourceWinforms.cs`
- `sources/engine/Stride.Input/UWP/InputSourceUWP.cs`
- `sources/engine/Stride.Input/Android/InputSourceAndroid.cs`
- `sources/engine/Stride.Input/iOS/InputSourceiOS.cs`
- `sources/engine/Stride.Games/Stride.Games.csproj`
- `sources/sdk/Stride.Build.Sdk/Sdk/Stride.Platform.props`
- `sources/sdk/Stride.Build.Sdk/Sdk/Stride.Frameworks.targets`
- `sources/sdk/Stride.Build.Sdk/Sdk/Stride.Platform.targets`
- `sources/sdk/Stride.Build.Sdk/Sdk/Stride.Graphics.targets`

### Parsing/scripts
- No custom parser script required; inspection done via `rg` + line-numbered file reads.

### Modification status
- Static-only analysis performed.
- No build/test run.
- No project/script patching done.
- No deletes.

### Static-only uncertainty
- This report infers compile-path behavior from SDK properties/conditions and source `#if` guards.
- It does **not** prove compile success (by constraint), native runtime behavior, or package restore edge-cases.

---

## 2) Current M1c baseline recap

Based on provided context (not re-validated in this task):
- M1c explicit graph includes six M1a core projects + `Stride`, `Stride.Games`, `Stride.Graphics` (+ transitive `Stride.FreeImage`, `Stride.Shaders`).
- Uses source-built AssemblyProcessor via bootstrap routing.
- Linux/Vulkan route (`StridePlatforms=Linux`, `StrideGraphicsApis=Vulkan`) built for `Stride.Games` and `Stride.Graphics`.

What M1c proves:
- Linux desktop runtime path for foundational/game/graphics layer is viable.
- Vulkan route and existing native dependencies were at least sufficient for that slice.

What M1c does not prove:
- `Stride.Input` project graph and package gating.
- SDL input code compile path selection.
- Windows-only input package inactivity.
- Gamepad/touch/mobile branch isolation for Linux.

---

## 3) `Stride.Input` project audit

Project: `sources/engine/Stride.Input/Stride.Input.csproj`

- **Direct project refs**: only `..\Stride.Games\Stride.Games.csproj`. (Direct.)
- **Direct package refs**:
  - `SharpDX.DirectInput` (condition: target framework contains `-windows`)
  - `SharpDX.XInput` (same condition)
  - `Microsoft.Management.Infrastructure` (same condition)
- **Conditional package refs**: all three above are windows-target gated.
- **Target framework behavior**:
  - `StrideRuntime=true` and SDK expands runtime TFMs.
  - `StrideExplicitWindowsRuntime=true`, but windows TFM is added only if `StridePlatforms` contains Windows.
- **Stride flags**:
  - `StrideRuntime=true`
  - `StrideExplicitWindowsRuntime=true`
  - `StrideGraphicsApiDependent=true`
  - `StrideAssemblyProcessor=true`
  - no explicit `StrideAssemblyProcessorOptions` override in Input project
- **Desktop UI props**:
  - `UseWPF` and `UseWindowsForms` only when TFM contains `-windows`.

Dependency implications:
- Directly requires `Stride.Games`.
- Transitively pulls `Stride.Graphics` through `Stride.Games`.
- No direct references to editor/assets/presentation/quantum/source-asset-compiler.
- No direct project references to rendering/audio/VR/shader compiler projects.
- New direct managed packages are windows-gated.

Linux feasibility (static): **High (with validation pending)**.

Recommendation: **Include** as next slice (Option A), with Linux/Vulkan + source-built AssemblyProcessor same as M1c.

---

## 4) Input source/platform audit

### SDL group
- Representative: `SDL/InputSourceSDL.cs`, `SDL/KeyboardSDL.cs`, `SDL/MouseSDL.cs`, `SDL/PointerSDL.cs`, `SDL/GameControllerSDL.cs`, `SDL/GamePadSDL.cs`.
- Mechanism: file-level `#if STRIDE_UI_SDL` for `InputSourceSDL` and factory switch branch.
- Purpose: desktop SDL mouse/keyboard/pointer/gamecontroller/gamepad path.
- Linux relevance: primary expected path.
- Risk: low-medium (depends on SDL + symbols being selected).

### Windows group
- Representative: `Windows/InputSourceWindowsDirectInput.cs`, `Windows/InputSourceWindowsXInput.cs`, `Windows/InputSourceWindowsRawInput.cs`, `Windows/InputSourceWinforms.cs`.
- Mechanism: guarded with `#if (STRIDE_UI_WINFORMS || STRIDE_UI_WPF)` (and some sub-guards like `STRIDE_INPUT_RAWINPUT`).
- Purpose: Win32/WinForms direct/raw/xinput backends.
- Linux relevance: should be excluded.
- Risk: medium if symbols/windows-TFM leak in.

### Android/iOS/UWP groups
- Android: `Android/InputSourceAndroid.cs` (`#if STRIDE_PLATFORM_ANDROID`), extends SDL path with sensors.
- iOS: `iOS/InputSourceiOS.cs` (`#if STRIDE_PLATFORM_IOS`), extends SDL path with CoreMotion/CoreLocation sensors.
- UWP: `UWP/InputSourceUWP.cs` (`#if STRIDE_PLATFORM_UWP`) with `Windows.*` APIs.
- Linux relevance: should be excluded under Linux-only platform list.
- Risk: low if `StridePlatforms=Linux` is enforced.

### Gamepad/touch/gesture/shared
- Shared abstractions and gesture logic are cross-platform and always relevant.
- SDL pointer/touch exists (`PointerSDL`) and platform-specific sensor code lives behind per-platform symbols.
- No `VirtualReality/` directory found in `Stride.Input` scan.

Answers:
- SDL input path real/buildable-looking: **Yes** (explicit classes + factory routing).
- SDL path entangled with Windows APIs: **Not directly** in SDL files; Windows APIs are in separately-guarded files.
- Windows-only packages/files gated: **Yes**, by `-windows` TFM package conditions + WINFORMS/WPF/UWP preprocessor guards.
- Mobile/UWP files gated for Linux: **Yes**, by `STRIDE_PLATFORM_ANDROID/IOS/UWP` guards.
- Need project split before validation? **No (static view)**; validate first with current project.

---

## 5) SDK/platform symbol behavior

- `StridePlatforms` defaults from host OS, and can be overridden CLI; Linux default/override supported.
- `StrideRuntime=true` produces runtime TFMs; windows TFM added only when `StridePlatforms` includes Windows **and** `StrideExplicitWindowsRuntime=true`.
- With `StridePlatforms=Linux`, expected runtime TFM set is Linux-compatible net10 (no `-windows`).
- `StrideUI` selection in graphics targets:
  - non-UWP target => includes `SDL`
  - `-windows` target can add `WINFORMS;WPF`
- `STRIDE_UI_SDL`, `STRIDE_UI_WINFORMS`, `STRIDE_UI_WPF` defined from `StrideUI` contents.
- Platform defines:
  - desktop TFMs => `STRIDE_PLATFORM_DESKTOP`
  - Android/iOS TFMs => corresponding mobile symbols.

Conclusion:
- `StridePlatforms=Linux` + `StrideGraphicsApis=Vulkan` should select SDL/Linux desktop path and avoid windows/mobile/UWP branches.
- `StrideExplicitWindowsRuntime=true` should not force windows TFM when platform list is Linux-only.
- CLI properties appear sufficient to force intended path cleanly.

---

## 6) Candidate M1d slice options

### Option A (recommended)
- Explicit projects: M1c explicit list + `sources/engine/Stride.Input/Stride.Input.csproj`.
- Likely transitives: existing M1c graph + windows-gated packages inactive on Linux.
- Excluded systems: engine/editor/assets/presentation/audio/rendering/VR/shader compiler/mobile slices.
- Expected blockers: symbol/TFM leakage, SDL/native package/runtime mismatch, AP routing mistakes.
- Proves: input layer graph integrates on Linux Vulkan.
- Does not prove: runtime input device behavior.

### Option B
- Add extra explicit transitives if static evidence required.
- Static evidence today does **not** show extra explicit project needed beyond Input+existing M1c.
- Recommendation: unnecessary complexity now.

### Option C
- Pre-split/patch Input for Linux-only before validation.
- Violates “one layer at a time” and adds prep churn without proof of need.
- Recommendation: defer unless Option A fails.

---

## 7) Proposed next implementation target

Smallest credible target: **Option A**.

- Proposed `.slnf`: `stride-m1d-input-linux.slnf` (name suggestion).
- Explicit projects to include:
  - existing M1c explicit project list
  - `sources/engine/Stride.Input/Stride.Input.csproj`
- Bootstrap: mirror existing M1c bootstrap pattern (source-built AssemblyProcessor routing), new script only if current scripts are slice-specific.
- Restore/build command pattern:

```bash
dotnet restore <M1d.slnf> -p:StridePlatforms=Linux -p:StrideGraphicsApis=Vulkan -p:StrideAssemblyProcessorFramework=net10.0 -p:StrideAssemblyProcessorBasePath=<sourcebuild-ap-path> -p:StrideAssemblyProcessorHash=sourcebuild

dotnet build <M1d.slnf> -c Debug -p:StridePlatforms=Linux -p:StrideGraphicsApis=Vulkan -p:StrideAssemblyProcessorFramework=net10.0 -p:StrideAssemblyProcessorBasePath=<sourcebuild-ap-path> -p:StrideAssemblyProcessorHash=sourcebuild
```

Expected first blockers (if any):
1. accidental windows TFM/pkgs activation;
2. symbol mismatch causing WinForms/UWP source inclusion;
3. AP routing drift;
4. SDL/native package/version mismatches.

---

## 8) Risk register

| Risk | Candidate/project | Likelihood | Impact | Evidence | Mitigation |
| ---- | ----------------- | ---------: | -----: | -------- | ---------- |
| Windows-only input packages accidentally active | `Stride.Input` | Medium | High | Package refs are `-windows` conditioned; safe only if no windows TFM enters graph. | Force `StridePlatforms=Linux`; inspect evaluated TFMs in first build logs. |
| `net10.0-windows` accidentally added | SDK frameworks expansion | Medium | High | Windows TFM added only when platforms include Windows and explicit flag true. | Keep Linux-only `StridePlatforms`; avoid mixed platform list. |
| SDL input symbols not selected | `Stride.Graphics.targets` + Input | Low-Med | High | Non-UWP sets `StrideUI=SDL`, defines `STRIDE_UI_SDL`. | Keep Linux net10 path + Vulkan; verify compile includes SDL files. |
| RawInput/DirectInput/XInput compiling on Linux | `Stride.Input/Windows/*` | Low-Med | Medium | Windows files guarded by WINFORMS/WPF; packages windows-TFM only. | Verify no WINFORMS/WPF symbols in Linux compile. |
| Mobile/UWP/touch files compile unexpectedly | Android/iOS/UWP input files | Low | Medium | Guarded by `STRIDE_PLATFORM_ANDROID/IOS/UWP`. | Keep `StridePlatforms=Linux`; verify no mobile TFMs. |
| Gamepad backend deps missing | SDL gamepad path | Medium | Medium | SDL joystick APIs used directly. | First M1d build/runtime smoke in next phase. |
| Native SDL dependency issues | SDL stack | Medium | Medium-High | Input SDL path depends on SDL subsystem init/joystick. | Validate in implementation build; keep environment parity with M1c. |
| AssemblyProcessor routing still required | Input + transitives | High | High | `StrideAssemblyProcessor=true` in Input + prior M1a/M1c pattern. | Reuse source-built AP bootstrap exactly. |
| `.slnf` misses required explicit project | M1d solution filter | Medium | High | Input directly references Games; transitives must remain reachable. | Start from M1c filter and append Input project only. |

---

## 9) Recommended implementation prompt

```text
Implement M1d as the smallest Linux-first input slice.

Constraints:
- Modify only solution-filter/bootstrap artifacts needed for M1d.
- Do not patch engine/sdk project files.
- Do not include Stride.Engine.
- Do not include editor/assets/presentation/mobile/rendering/audio/VR/shader-compiler slices.
- Keep source-built AssemblyProcessor routing pattern used by M1a/M1c.

Tasks:
1) Create new .slnf named stride-m1d-input-linux.slnf (or repo naming equivalent).
2) Base it on the validated M1c explicit project set and add exactly:
   - sources/engine/Stride.Input/Stride.Input.csproj
3) Add/update bootstrap script only if needed to mirror existing AP sourcebuild wiring.
4) Run restore/build for M1d slice only using:
   - -p:StridePlatforms=Linux
   - -p:StrideGraphicsApis=Vulkan
   - -p:StrideAssemblyProcessorFramework=net10.0
   - -p:StrideAssemblyProcessorBasePath=<sourcebuild path>
   - -p:StrideAssemblyProcessorHash=sourcebuild
5) If build fails, stop at first blocker cluster and report exact failing project, target framework, graphics API, symbol/package path causing failure.
6) Do not fix unrelated issues.
```

