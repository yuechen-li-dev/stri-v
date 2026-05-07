# 1130 — M10a Stride.Input 5S Sort Validation

Date: 2026-05-07

## 1) Files changed

### Modified
- `striv/projects/Stride.Input/Stride.Input.csproj`

### Moved (active organization)
- `striv/projects/Stride.Input/VirtualButtonConfigShim.cs` -> `striv/projects/Stride.Input/Compatibility/VirtualButtonConfigShim.cs`

### Moved to Obsolete quarantine (out of active compile path)
- `striv/projects/Stride.Input/Windows/DirectInputJoystick.cs` -> `striv/projects/Stride.Input/Obsolete/WindowsControllers/DirectInputJoystick.cs`
- `striv/projects/Stride.Input/Windows/DirectInputState.cs` -> `striv/projects/Stride.Input/Obsolete/WindowsControllers/DirectInputState.cs`
- `striv/projects/Stride.Input/Windows/GameControllerDirectInput.cs` -> `striv/projects/Stride.Input/Obsolete/WindowsControllers/GameControllerDirectInput.cs`
- `striv/projects/Stride.Input/Windows/GamePadDirectInput.cs` -> `striv/projects/Stride.Input/Obsolete/WindowsControllers/GamePadDirectInput.cs`
- `striv/projects/Stride.Input/Windows/GamePadXInput.cs` -> `striv/projects/Stride.Input/Obsolete/WindowsControllers/GamePadXInput.cs`
- `striv/projects/Stride.Input/Windows/InputSourceWindowsDirectInput.cs` -> `striv/projects/Stride.Input/Obsolete/WindowsControllers/InputSourceWindowsDirectInput.cs`
- `striv/projects/Stride.Input/Windows/InputSourceWindowsXInput.cs` -> `striv/projects/Stride.Input/Obsolete/WindowsControllers/InputSourceWindowsXInput.cs`

Deleted files: none.

## 2) 5S phase

M10a is **Sort** only. This pass organized the now-local `Stride.Input` tree under `striv/projects/Stride.Input`, separated clearly out-of-scope desktop-excluded backend files into an Obsolete quarantine folder, and updated compile boundaries. No Shine/nullability-warning cleanup or behavior refactor was performed.

## 3) Input scope (current Stri-V)

Retained in active compile scope:
- Linux desktop SDL path (`SDL/**`)
- Windows desktop RawInput path (`Windows/RawInput/**` + RawInput-based source/device files)
- Simulated input path (`Simulated/**`)
- Common abstractions/events/devices/gamepad layouts and compatibility surface needed for existing API/build

Excluded from active scope:
- Android/iOS/UWP (already removed before this task)
- VirtualButton runtime system (not restored)
- DirectInput/XInput backends (quarantined to `Obsolete/` and excluded from compile)

## 4) Organization plan applied

- Added explicit compatibility folder:
  - `Compatibility/VirtualButtonConfigShim.cs`
- Added explicit quarantine folder:
  - `Obsolete/WindowsControllers/*` for DirectInput/XInput stack
- Project-file policy changed from ad-hoc file removal to boundary-based exclusion:
  - `Compile Include="**/*.cs"` with `Exclude` extended to include `**/ToBeDeleted/**;**/Obsolete/**`
- Namespace policy:
  - Kept existing namespaces/type names unchanged to avoid public API churn.
  - Physical folder moves only.

## 5) Cull/removal audit

| Area/file group | Decision | Reason | Action |
|---|---|---|---|
| Windows DirectInput backend (`DirectInput*`, `GameControllerDirectInput`, `GamePadDirectInput`, `InputSourceWindowsDirectInput`) | Move to Obsolete | Explicitly out of current scope; previously only partially excluded via csproj removes | Moved to `Obsolete/WindowsControllers/` and excluded by glob |
| Windows XInput backend (`GamePadXInput`, `InputSourceWindowsXInput`) | Move to Obsolete | Explicitly out of current scope unless proven needed | Moved to `Obsolete/WindowsControllers/` and excluded by glob |
| RawInput backend (`Windows/RawInput/**`, `InputSourceWindowsRawInput`, `KeyboardWindowsRawInput`) | Keep | Required for minimal Windows desktop path | Left active |
| SDL backend (`SDL/**`) | Keep | Required for Linux desktop path | Left active |
| Simulated backend (`Simulated/**`) | Keep | Explicitly retained scope | Left active |
| VirtualButton compatibility shim | Keep | Needed to preserve `InputManager` API compile compatibility after VirtualButton runtime removal | Moved to `Compatibility/` |
| Gesture/sensor APIs | Defer | Potentially public surface; out-of-scope cleanup could be breaking and is not required for Sort pass | No removal in M10a |

## 6) Compatibility shims

- `VirtualButtonConfigShim.cs` remains present and still necessary to keep `InputManager` public API compile-compatible without reintroducing VirtualButton runtime implementation.
- It now lives at `striv/projects/Stride.Input/Compatibility/VirtualButtonConfigShim.cs`.

## 7) Build/test results

| Command | Exit code | First meaningful warning/error | Pass/Fail | Output truncated |
|---|---:|---|---|---|
| `dotnet build striv/projects/Stride.Input/Stride.Input.csproj -c Debug -p:StriVWarningFocusProject=Stride.Input 2>&1 | tee /tmp/striv-m10a-input-sort-build.log` | 0 | `warning CS8765` in `GamePadState.cs` | Pass | No |
| `dotnet build striv/StriV.Core.slnx -c Debug -p:StriVWarningFocusProject=Stride.Input 2>&1 | tee /tmp/striv-m10a-input-sort-slnx-build.log` | 0 | none | Pass | No |
| `dotnet test striv/tests/StriV.AssetTool.Tests/StriV.AssetTool.Tests.csproj -v minimal` | 0 | none | Pass | No |
| `dotnet test striv/tests/StriV.AssetPipeline.Tests/StriV.AssetPipeline.Tests.csproj --no-build -v minimal` | 0 | none | Pass | No |
| `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj --no-build -v minimal` | 0 | one existing skipped test (`StreamLiveness_DoesNotPruneWhenAccessUnknown`) | Pass | No |
| `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj -v minimal` | 0 | none | Pass | No |
| `./striv/build/striv-build-core.sh` | 0 | none | Pass | No |

## 8) Focused warning snapshot

Commands run:
- `grep -E "warning (CS|CA|NU|STRIDE)[0-9]+" /tmp/striv-m10a-input-sort-build.log | grep "Stride.Input" > /tmp/striv-m10a-input-warning-lines.log || true`
- `wc -l /tmp/striv-m10a-input-warning-lines.log`
- `sed -E 's/.*warning ((CS|CA|NU|STRIDE)[0-9]+).*/\1/' /tmp/striv-m10a-input-warning-lines.log | sort | uniq -c | sort -nr`

Results after Sort:
- Focused warning lines: **164**
- Top warning codes:
  - `CS8618` (70)
  - `CS8602` (22)
  - `CS8601` (22)
  - `CS8604` (12)
  - `CS8600` (12)

Comparison to prior snapshot (164): unchanged warning-line count, which is expected for Sort-only work.

## 9) Deferred work

- M10b Set-in-order pass for clearer long-term folder taxonomy across remaining active files.
- Shine/nullability warning cleanup (`CS8618/CS8602/CS8601/...`).
- Windows RawInput runtime smoke validation.
- Linux SDL runtime smoke validation.
- Controller support strategy decision (if/when to permanently drop or selectively reintroduce XInput).
- Human mass-delete of `Obsolete/WindowsControllers/*` once final confidence is confirmed.

## 10) Recommended next task

**Recommended:** M10b Set-in-order for `Stride.Input`, now that out-of-scope Windows controller backends are quarantined and compile-excluded by directory policy.
