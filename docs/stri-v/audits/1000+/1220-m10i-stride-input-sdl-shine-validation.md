# 1220 M10i — Stride.Input SDL Shine validation

## 1) Files changed
- `striv/projects/Stride.Input/Sources/SDL/InputSourceSDL.cs`
- `striv/projects/Stride.Input/Sources/SDL/GameControllerSDL.cs`
- `striv/projects/Stride.Input/Sources/SDL/GamePadSDL.cs`

## 2) Task scope
- M10i target was SDL lifecycle/device/event nullability in `Stride.Input/Sources/SDL/**` only.
- `InputManager` remained untouched and warning-clean (0 focused warnings in this pass).
- Gesture pipeline, DTO initialization, and `InputEventPool` were intentionally deferred.

## 3) Before warnings
- Focused warnings before: **60** lines in filtered warning log (`/tmp/striv-m10i-input-warning-lines-before.log`).
- Distribution before:
  - CS8618: 36
  - CS8602: 20
  - CS8625: 4
- SDL warnings before: **14** lines in `/tmp/striv-m10i-sdl-warnings-before.log` (duplicated summary+diagnostic lines; 7 unique sites):
  - `Sources/SDL/GamePadSDL.cs` CS8602
  - `Sources/SDL/InputSourceSDL.cs` CS8618 (`mouse`, `keyboard`, `pointer`, `inputManager`)
  - `Sources/SDL/GameControllerSDL.cs` CS8618 (`Disconnected`)
  - `Sources/SDL/GameControllerSDL.cs` CS8625 (`Invoke(..., null)`)

## 4) SDL lifecycle map
- `InputSourceSDL` owns SDL joystick subsystem lifecycle (`InitSubSystem` in `Initialize`, `QuitSubSystem` in `Dispose`), and owns desktop devices (`MouseSDL`, `KeyboardSDL`, `PointerSDL`) created during `Initialize`.
- Controller discovery uses `Scan()` / `OpenDevice()`, with `joystickInstanceIdToDeviceId` tracking and deferred removals through `devicesToRemove` in `Update()`.
- `GameControllerSDL` owns a raw `Joystick*`, publishes `Disconnected`, and closes joystick in `Dispose()`.
- `GamePadSDL` wraps `GameControllerSDL` through `GamePadFromLayout.GameControllerDevice`; dispose path forwards to SDL controller dispose.
- SDL APIs can produce lifecycle-dependent state (pre/post-initialize and disconnect) not visible to compiler flow analysis, which caused the targeted nullable warnings.

## 5) Fixes applied
### `InputSourceSDL.cs`
- Addressed CS8618 for constructor-exiting uninitialized lifecycle fields.
- Made lifecycle-initialized fields nullable (`MouseSDL?`, `KeyboardSDL?`, `PointerSDL?`, `InputManager?`) and documented they are set in `Initialize()`.
- Added early `Dispose()` guard for pre-initialize disposal path (no-op base-dispose path).
- Added explicit `InvalidOperationException` guard before creating `GamePadSDL` when `inputManager` is unexpectedly null.
- Behavior impact: no intended backend behavior change; changes are lifecycle-nullability truth + defensive guards.

### `GameControllerSDL.cs`
- Addressed CS8618 by making `Disconnected` event nullable (`EventHandler?`).
- Addressed CS8625 by invoking with `EventArgs.Empty` instead of `null`.
- Preserved existing disconnect-contract enforcement (`throw` if nobody subscribed).

### `GamePadSDL.cs`
- Addressed CS8602 by replacing `(GameControllerDevice as GameControllerSDL).Dispose()` with guarded pattern match.
- Behavior impact: disposal forwarding remains unchanged when wrapped controller is SDL; now null-safe for compiler and runtime.

## 6) Tests
- Ran existing tests:
  - `dotnet test striv/tests/Stride.Input.Tests/Stride.Input.Tests.csproj -v minimal` (pass).
- No new SDL-native tests were added because this pass was nullable/lifecycle guard focused and must avoid requiring real SDL runtime/window/device.

## 7) After warnings
- Focused warnings after: **46** lines in `/tmp/striv-m10i-input-warning-lines-after.log`.
- Distribution after:
  - CS8618: 26
  - CS8602: 18
  - CS8625: 2
- SDL warnings after: **0** lines in `/tmp/striv-m10i-sdl-warnings-after.log`.
- Checker result: `striv-check-focused-project.sh Stride.Input` reports focused warning count 46 but exits code 0.
- Checker exit-code contract issue remains present (warningful focused run returns success exit).

## 8) Deferred warnings
Remaining focused warnings are now non-SDL buckets:
- Gestures/event pipeline (`Gestures/*`, mostly CS8602).
- Device/event DTO initialization (`Devices/*`, `Events/*`, mostly CS8618).
- `InputEventPool.cs` (CS8602/CS8625).
- Remaining simulated-source warning (`Sources/Simulated/GamePadSimulated.cs` CS8618).
- Remaining SDL warnings: none in focused warning filter.

## 9) Validation results
| Command | Exit code | First meaningful warning/error | Pass/Fail | Output truncated |
|---|---:|---|---|---|
| `dotnet build striv/projects/Stride.Input/Stride.Input.csproj -c Debug -p:StriVWarningFocusProject=Stride.Input --no-incremental` | 0 | CS8618 in `GameControllerDeviceBase.cs` (non-SDL) | Pass | No |
| `./striv/build/striv-check-focused-project.sh Stride.Input` (before) | 0 | Reports focused warning count 60 | **Fail (contract bug)** | No |
| `find striv/projects/Stride.Input/Sources/SDL -type f | sort` | 0 | n/a | Pass | No |
| `rg -n "class .*SDL|..." striv/projects/Stride.Input/Sources/SDL` | 0 | n/a | Pass | No |
| `dotnet test striv/tests/Stride.Input.Tests/Stride.Input.Tests.csproj -v minimal` | 0 | n/a | Pass | No |
| `dotnet build striv/projects/Stride.Input/Stride.Input.csproj -c Debug -p:StriVWarningFocusProject=Stride.Input --no-incremental` (after) | 0 | CS8618 in non-SDL files; SDL filtered warnings 0 | Pass | No |
| `./striv/build/striv-check-focused-project.sh Stride.Input` (after) | 0 | Reports focused warning count 46 | **Fail (contract bug)** | No |
| `dotnet build striv/StriV.Core.slnx -c Debug -p:StriVWarningFocusProject=Stride.Input --no-incremental` | 0 | Focused warnings remain in non-SDL files | Pass | No |
| `dotnet test striv/tests/StriV.AssetTool.Tests/StriV.AssetTool.Tests.csproj -v minimal` | 0 | n/a | Pass | No |
| `dotnet test striv/tests/StriV.AssetPipeline.Tests/StriV.AssetPipeline.Tests.csproj --no-build -v minimal` | 0 | n/a | Pass | No |
| `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj --no-build -v minimal` | 0 | one skipped test (existing) | Pass | No |
| `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj -v minimal` | 1 (masked by `|| true`) | Timeout: `FocusedWarningLane_BepuPhysics_HasZeroWarnings` after 30s | Fail (known harness timeout class) | No |
| `./striv/build/striv-build-core.sh` | 0 | Large unrelated repository warning volume | Pass | **Yes** (tool output truncated) |

## 10) Recommended next task
**M10j checker exit-code contract repair** first, because focused checks currently return success with non-zero warning counts, which blocks reliable sustain gating. Then continue with **M10j gesture/event pipeline tests + Shine** on remaining non-SDL focused warnings.
