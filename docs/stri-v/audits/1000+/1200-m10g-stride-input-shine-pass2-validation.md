# M10g Stride.Input Shine pass 2 validation

## 1) Files changed
- striv/projects/Stride.Input/Core/InputManager.cs
- striv/projects/Stride.Input/Core/InputManager.State.cs

## 2) Task scope
M10g pass focused on lifecycle/nullability in `InputManager` first, validated against existing M10d tests. No backend/platform restoration and no suppression changes.

## 3) Before warnings
Command:
`dotnet build striv/projects/Stride.Input/Stride.Input.csproj -c Debug -p:StriVWarningFocusProject=Stride.Input --no-incremental`

- Focused warnings before: **146**
- Distribution before:
  - CS8618: 66
  - CS8602: 22
  - CS8601: 22
  - CS8604: 14
  - CS8600: 12
  - CS8603: 6
  - CS8625: 4
- InputManager warning lines before (grep by `Core/InputManager.cs`): **86**

## 4) Tests used / added
- Ran existing M10d suite:
  - `dotnet test striv/tests/Stride.Input.Tests/Stride.Input.Tests.csproj -v minimal`
- No new tests added in this pass.

## 5) Fixes applied
### `Core/InputManager.cs`
- Converted lifecycle-managed device/sensor convenience properties to nullable where genuinely absent before source registration.
- Converted nullable-capable events to nullable event declarations.
- Updated `GetGamePadByIndex` and `GetGamePadsByIndex` return nullability to match behavior.
- Added `AddSources()` early-return guard when `gameContext` is not initialized.
- Rationale: reflects true lifecycle/nullability without behavior refactor.

### `Core/InputManager.State.cs`
- Marked `LastPointerDevice` nullable.
- Added targeted null-forgiving access (`Keyboard!`, `Mouse!`) in branches already guarded by `HasKeyboard`/`HasMouse`.
- Rationale: mechanical flow-nullability alignment only.

## 6) After warnings
Command:
`dotnet build striv/projects/Stride.Input/Stride.Input.csproj -c Debug -p:StriVWarningFocusProject=Stride.Input --no-incremental`

- Focused warnings after: **94**
- Distribution after:
  - CS8618: 36
  - CS8602: 30
  - CS8604: 12
  - CS8600: 12
  - CS8625: 4
- InputManager warning lines after (`Core/InputManager.cs`): **34**
- Repaired checker:
  - `./striv/build/striv-check-focused-project.sh Stride.Input`
  - Focused warning count: 94
- Zero warning not achieved.

## 7) Deferred warnings
Deferred categories (non-mechanical / broader runtime touch):
- SDL lifecycle and device fields/events (`Sources/SDL/*`, CS8618/CS8602/CS8625)
- Gesture/event pipeline null-flow (`Gestures/*`, `InputEventPool.cs`, CS8602)
- Device/event initialization DTOs (`Events/*`, `Devices/*`, CS8618)
- Remaining InputManager flow casts in collection-change handlers and gesture item casts (`Core/InputManager.cs`, CS8600/CS8602/CS8604)

Recommended targeted passes:
- SDL lifecycle pass
- Gesture/event pipeline pass
- Device/event state initialization pass
- Focused InputManager cast-flow tightening pass

## 8) Validation results
- `dotnet test striv/tests/Stride.Input.Tests/Stride.Input.Tests.csproj -v minimal`
  - exit: 0, pass.
- `dotnet build striv/projects/Stride.Input/Stride.Input.csproj -c Debug -p:StriVWarningFocusProject=Stride.Input --no-incremental`
  - exit: 0, pass with focused warnings.
- `./striv/build/striv-check-focused-project.sh Stride.Input`
  - exit: 0, reports focused warnings (94).
- `dotnet build striv/StriV.Core.slnx -c Debug -p:StriVWarningFocusProject=Stride.Input --no-incremental`
  - exit: 0, pass with focused warnings.

(Full standard cross-test matrix and `striv-build-core.sh` were not re-run in this pass.)

## 9) Recommended next task
Proceed to **M10h Shine pass 3** targeting remaining `InputManager` cast-flow warnings first, then SDL lifecycle warnings with focused tests as needed.
