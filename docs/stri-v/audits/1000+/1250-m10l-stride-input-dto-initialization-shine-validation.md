# 1250 — M10l Stride.Input DTO initialization Shine validation

## 1) Files changed
- striv/projects/Stride.Input/Devices/GameController/GameControllerObjectInfo.cs
- striv/projects/Stride.Input/Devices/GameController/GameControllerDeviceBase.cs
- striv/projects/Stride.Input/Devices/GamePad/GamePadDeviceBase.cs
- striv/projects/Stride.Input/Sources/Simulated/GamePadSimulated.cs
- striv/projects/Stride.Input/Devices/Pointer/PointerDeviceBase.cs
- striv/projects/Stride.Input/Devices/Pointer/PointerPoint.cs
- striv/projects/Stride.Input/Events/DeviceChangedEventArgs.cs
- striv/projects/Stride.Input/Events/InputPreUpdateEventArgs.cs
- striv/projects/Stride.Input/Events/InputEvent.cs
- striv/projects/Stride.Input/Events/TextInputEvent.cs

## 2) Task scope
M10l targeted remaining Stride.Input DTO/device/simulated initialization warnings (CS8618) after prior InputManager, SDL, gesture recognizer, and InputEventPool null-flow cleanup work.

## 3) Before warnings
- Total focused warning lines before: **26**
- Distribution: **26 × CS8618**
- Unique warning sites (13, duplicated in output summary):
  - Devices/GameController/GameControllerObjectInfo.cs: Name
  - Devices/GameController/GameControllerDeviceBase.cs: ButtonStates/AxisStates/DirectionStates
  - Devices/GamePad/GamePadDeviceBase.cs: IndexChanged event
  - Sources/Simulated/GamePadSimulated.cs: Name property
  - Devices/Pointer/PointerDeviceBase.cs: SurfaceSizeChanged event
  - Devices/Pointer/PointerPoint.cs: Pointer
  - Events/DeviceChangedEventArgs.cs: Source/Device
  - Events/InputPreUpdateEventArgs.cs: GameTime
  - Events/InputEvent.cs: Device
  - Events/TextInputEvent.cs: Text

## 4) Initialization classification
| File/type | Warning | Classification | Fix strategy |
| --- | --- | --- | --- |
| GameControllerObjectInfo.Name | CS8618 field | dispatch-time required DTO member | safe default `string.Empty` |
| GameControllerDeviceBase state arrays | CS8618 fields | lifecycle-initialized device field | `null!` + lifecycle comment (initialized via `InitializeButtonStates`) |
| GamePadDeviceBase.IndexChanged | CS8618 event | public API compatibility shape | nullable event delegate |
| GamePadSimulated.Name | CS8618 property | simulated source/device lifecycle | initialize in constructor (`"Simulated GamePad"`) |
| PointerDeviceBase.SurfaceSizeChanged | CS8618 event | public API compatibility shape | nullable event delegate |
| PointerPoint.Pointer | CS8618 field | lifecycle-initialized device field | `null!` + lifecycle comment |
| DeviceChangedEventArgs.Source/Device | CS8618 fields | dispatch-time required DTO member | `null!` + producer-lifecycle comments |
| InputPreUpdateEventArgs.GameTime | CS8618 field | dispatch-time required DTO member | `null!` + producer-lifecycle comment |
| InputEvent.Device | CS8618 property | pooled event lifecycle | `null!` + producer/pool comment |
| TextInputEvent.Text | CS8618 field | nullable/optional payload (empty text valid) | safe default `string.Empty` |

## 5) Fixes applied
All fixes are nullability/initialization-only and preserve event semantics and device registration/update behavior.

## 6) Tests
- Ran existing Stride.Input tests before/after changes.
- No new tests added: changes were mechanical nullability/initialization updates with unchanged runtime control flow.

## 7) After warnings
- Total focused warning lines after: **0**
- Focused checker status: **0**
- Stride.Input is now zero-warning under focused lane.

## 8) Validation results
- `dotnet build striv/projects/Stride.Input/Stride.Input.csproj -c Debug -p:StriVWarningFocusProject=Stride.Input --no-incremental` → exit 0, pass, no warnings, not truncated.
- `dotnet test striv/tests/Stride.Input.Tests/Stride.Input.Tests.csproj -v minimal` → exit 0, pass, not truncated.
- `./striv/build/striv-check-focused-project.sh Stride.Input` → exit 0, pass, focused warning count 0, not truncated.
- `dotnet build striv/StriV.Core.slnx -c Debug -p:StriVWarningFocusProject=Stride.Input --no-incremental` → exit 0, pass, not truncated.
- `dotnet test striv/tests/StriV.AssetTool.Tests/StriV.AssetTool.Tests.csproj -v minimal` → exit 0, pass, not truncated.
- `dotnet test striv/tests/StriV.AssetPipeline.Tests/StriV.AssetPipeline.Tests.csproj --no-build -v minimal` → exit 0, pass, not truncated.
- `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj --no-build -v minimal` → exit 0, pass, not truncated.
- `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj -v minimal` → timeout-related failure in focused lane (`Stride.BepuPhysics`), known harness issue per instruction.
- `./striv/build/striv-build-core.sh` → exit 0, pass, output truncated in terminal capture due length.

## 9) Deferred work
- SDL runtime validation.
- Windows RawInput runtime validation.
- controller/XInput policy.
- deletion of `Obsolete/WindowsControllers`.
- no remaining focused warnings in Stride.Input.

## 10) Recommended next task
Proceed to **M10m Standardize/Sustain for Stride.Input**.
