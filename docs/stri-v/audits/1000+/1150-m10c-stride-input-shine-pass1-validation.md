# M10c – Stride.Input Shine pass 1 validation

## 1) Files changed
- `striv/projects/Stride.Input/Devices/Sensors/Direction.cs`
- `striv/projects/Stride.Input/Devices/GamePad/GamePadState.cs`
- `striv/projects/Stride.Input/Core/InputSourceFactory.cs`
- `striv/projects/Stride.Input/Layouts/GamePadLayouts.cs`
- `striv/projects/Stride.Input/Core/InputManager.cs`
- `docs/stri-v/audits/1000+/1150-m10c-stride-input-shine-pass1-validation.md`

## 2) 5S phase
M10c is **Shine pass 1** for `Stride.Input`: this pass intentionally focused on easy/mechanical nullability cleanup and precise lifecycle documentation, not total warning elimination.

- Fixed now: low-risk signature/annotation mismatches and truthful nullable return contracts.
- Deferred: lifecycle/platform/runtime-sensitive warnings in `InputManager`, SDL/RawInput, gesture/event pipeline.

## 3) Before warnings
Command:

```bash
dotnet build striv/projects/Stride.Input/Stride.Input.csproj -c Debug -p:StriVWarningFocusProject=Stride.Input 2>&1 | tee /tmp/striv-m10c-input-before.log
grep -E "warning (CS|CA|NU|STRIDE)[0-9]+" /tmp/striv-m10c-input-before.log | grep "Stride.Input" > /tmp/striv-m10c-input-warning-lines-before.log || true
wc -l /tmp/striv-m10c-input-warning-lines-before.log
sed -E 's/.*warning ((CS|CA|NU|STRIDE)[0-9]+).*/\1/' /tmp/striv-m10c-input-warning-lines-before.log | sort | uniq -c | sort -nr
```

Focused warning count before: **164**.

Code distribution before:
- CS8618: 70
- CS8602: 22
- CS8601: 22
- CS8604: 12
- CS8600: 12
- CS8603: 10
- CS8622: 8
- CS8765: 4
- CS8625: 4

Representative sites:
- `Devices/Sensors/Direction.cs` (`CS8765`)
- `Devices/GamePad/GamePadState.cs` (`CS8765`)
- `Core/InputManager.cs` (mixed lifecycle-heavy `CS8618/CS860x/CS8622`)

## 4) Warning categorization

| Category | Warning codes | Example files | Decision |
| -------- | ------------- | ------------- | -------- |
| Easy/mechanical now | CS8765, CS8603 | `Direction.cs`, `GamePadState.cs`, `InputSourceFactory.cs`, `GamePadLayouts.cs` | Fixed now |
| Lifecycle comment + defer | CS8618, CS8601 | `Core/InputManager.cs`, `Sources/SDL/InputSourceSDL.cs` | Commented/deferred |
| Needs runtime/platform validation | CS8602, CS8625 | `Sources/SDL/GamePadSDL.cs`, `Sources/SDL/GameControllerSDL.cs`, RawInput paths in `InputManager.cs` | Deferred |
| Needs test before fix | CS8602 in gesture/event flow | `Gestures/*`, `InputEventPool.cs` | Deferred pending targeted tests |
| Obsolete/removed-scope fallout | N/A in this pass | `Obsolete/WindowsControllers/*` excluded scope | Deferred |

## 5) Fixes applied
- `Direction.cs`
  - Addressed `CS8765`.
  - Changed `Equals(object obj)` to `Equals(object? obj)`.
  - Behavior unchanged: equality logic remains identical.

- `GamePadState.cs`
  - Addressed `CS8765`.
  - Changed `Equals(object obj)` to `Equals(object? obj)`.
  - Behavior unchanged.

- `InputSourceFactory.cs`
  - Addressed `CS8603` truthfully.
  - `NewWindowInputSource` return type changed to `IInputSource?` because `Headless` explicitly returns `null`.
  - Behavior unchanged; contract now matches existing sentinel usage.

- `GamePadLayouts.cs`
  - Addressed `CS8603` truthfully.
  - `FindLayout` return type changed to `GamePadLayout?` because function already returns `null` when no layout matches.
  - Behavior unchanged.

- `InputManager.cs`
  - Addressed mechanical delegate nullability mismatches (`CS8622`) by changing event handler sender parameters to nullable (`object?`).
  - Added targeted lifecycle comments on `gameContext` initialization and compatibility intent for `VirtualButtonConfigSet`.
  - Made `gameContext` and `VirtualButtonConfigSet` nullable to reflect existing lifecycle/compatibility truth rather than forcing `null!`.

## 6) Hard warnings deferred
Remaining warnings were intentionally deferred where fix requires lifecycle or runtime confidence:
- `Core/InputManager.cs`: constructor-time non-null properties/events (`CS8618`), source/device flow (`CS8600/CS8601/CS8604`), and nullable dereference risks (`CS8602`).
- `Sources/SDL/*`: SDL controller/device disconnection/state ownership nullability (`CS8618`, `CS8602`, `CS8625`).
- `Gestures/*` and `InputEventPool.cs`: nullability in gesture/event queue logic (`CS8602`, `CS8625`) needing targeted tests.
- `Events/*` and device/event payload classes: lifecycle-populated members (`CS8618`) best handled with explicit initialization strategy in focused pass.

Future targeted pass needs:
- InputManager lifecycle test scaffolding around source registration/device callbacks.
- SDL runtime-validation pass for controller connect/disconnect ownership.
- Gesture pipeline tests before changing nullability contracts in recognizers/event pool.

## 7) After warnings
Command:

```bash
dotnet build striv/projects/Stride.Input/Stride.Input.csproj -c Debug -p:StriVWarningFocusProject=Stride.Input 2>&1 | tee /tmp/striv-m10c-input-after.log
grep -E "warning (CS|CA|NU|STRIDE)[0-9]+" /tmp/striv-m10c-input-after.log | grep "Stride.Input" > /tmp/striv-m10c-input-warning-lines-after.log || true
wc -l /tmp/striv-m10c-input-warning-lines-after.log
sed -E 's/.*warning ((CS|CA|NU|STRIDE)[0-9]+).*/\1/' /tmp/striv-m10c-input-warning-lines-after.log | sort | uniq -c | sort -nr
```

Focused warning count after: **146**.

Distribution after:
- CS8618: 66
- CS8602: 22
- CS8601: 22
- CS8604: 14
- CS8600: 12
- CS8603: 6
- CS8625: 4

Delta vs prior snapshot (164): **-18 warning lines**.

Zero-warning status: **Not achieved in Shine pass 1 (expected).**

## 8) Tests
No new tests were added because code changes were signature/annotation/comments-only and behavior-preserving.

## 9) Validation results
- `dotnet build striv/projects/Stride.Input/Stride.Input.csproj -c Debug -p:StriVWarningFocusProject=Stride.Input`  
  Exit: 0; first meaningful warning: `CS8618` in `Core/InputManager.cs`; pass (with warnings); output truncated: no.

- `dotnet build striv/StriV.Core.slnx -c Debug -p:StriVWarningFocusProject=Stride.Input`  
  Exit: 0; first meaningful warning/error: none; pass; output truncated: no.

- `dotnet test striv/tests/StriV.AssetTool.Tests/StriV.AssetTool.Tests.csproj -v minimal`  
  Exit: 0; first meaningful warning/error: none; pass; output truncated: no.

- `dotnet test striv/tests/StriV.AssetPipeline.Tests/StriV.AssetPipeline.Tests.csproj --no-build -v minimal`  
  Exit: 0; first meaningful warning/error: none; pass; output truncated: no.

- `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj --no-build -v minimal`  
  Exit: 0; first meaningful warning/error: one pre-existing skipped test (`StreamLiveness_DoesNotPruneWhenAccessUnknown`); pass; output truncated: no.

- `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj -v minimal`  
  Exit: 0; first meaningful warning/error: none; pass; output truncated: no.

- `./striv/build/striv-build-core.sh`  
  Exit: 0; first meaningful warning/error: none; pass; output truncated: no.

## 10) Recommended next task
**M10d targeted InputManager lifecycle test pass**, then a second focused Shine pass for `CS8618/CS8601/CS8604` in registration/update ownership paths.
