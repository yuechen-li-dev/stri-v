# M10h Stride.Input InputManager cast-flow/null-flow Shine validation

## 1) Files changed
- `striv/projects/Stride.Input/Core/InputManager.cs`
- `docs/stri-v/audits/1000+/1210-m10h-stride-input-inputmanager-castflow-shine-validation.md`

## 2) Task scope
This M10h pass targeted only remaining `InputManager` cast-flow/null-flow warnings in `Core/InputManager.cs` (bucket 1).

Intentionally **not targeted** in this pass:
- SDL lifecycle/device/event warnings.
- RawInput lifecycle warnings.
- Gesture pipeline warnings outside `InputManager` itself.
- Device/event DTO initialization warnings.

## 3) Before warnings
Command:
```bash
dotnet build striv/projects/Stride.Input/Stride.Input.csproj -c Debug \
  -p:StriVWarningFocusProject=Stride.Input \
  --no-incremental \
  2>&1 | tee /tmp/striv-m10h-input-before.log
```

Focused warning lines before: **94** (`wc -l /tmp/striv-m10h-input-warning-lines-before.log`).

Warning code distribution before:
- CS8618: 36
- CS8602: 30
- CS8604: 12
- CS8600: 12
- CS8625: 4

`InputManager` warning lines before: **34** (`wc -l /tmp/striv-m10h-inputmanager-warnings-before.log`; duplicated in build log emission), with unique sites in `Core/InputManager.cs` at lines:
- 156, 372, 384, 461, 602, 609, 610, 615, 748, 751, 779, 788, 791.

## 4) Tests used / added
Executed M10d test suite before and after changes:
```bash
dotnet test striv/tests/Stride.Input.Tests/Stride.Input.Tests.csproj -v minimal
```
Result: Passed (5/5).

No new tests were added. Rationale: changes are pattern-matching/null-guard rewrites in existing collection/event plumbing without intended behavior changes, and lifecycle/duplicate-add behavior remains covered by the existing `Stride.Input.Tests` suite.

## 5) Fixes applied
### `striv/projects/Stride.Input/Core/InputManager.cs`
Warnings addressed: remaining `InputManager` CS8600/CS8602/CS8604 set.

Applied patterns:
- Nullable-safe property/method access for mouse usage (`Mouse?.…`, `Mouse?.IsPositionLocked ?? false`).
- `TryGetValue(..., out var router)` style to avoid nullable out-local warning.
- Pattern matching guards for collection items:
  - `if (e.Item is not IInputSource source) throw ...`
  - `if (e.Item is IInputDevice addedDevice) ...`
  - `if (trackingCollectionChangedEventArgs.Item is GestureConfig addConfig) ...`
- Safe dictionary lookup on remove path (`TryGetValue` before unsubscribe/remove).
- Removed unused lambda sender identifier (`_`) in source-device event hook.

Behavior preservation:
- Source registration/update order unchanged.
- Device add/remove routing semantics unchanged for valid typed items.
- Existing duplicate-source guard retained.
- Gestures still react to add/remove actions only; unsupported actions unchanged.

## 6) After warnings
Commands:
```bash
dotnet build striv/projects/Stride.Input/Stride.Input.csproj -c Debug \
  -p:StriVWarningFocusProject=Stride.Input \
  --no-incremental \
  2>&1 | tee /tmp/striv-m10h-input-after.log

./striv/build/striv-check-focused-project.sh Stride.Input || true
```

Focused warning lines after: **60**.

Warning code distribution after:
- CS8618: 36
- CS8602: 20
- CS8625: 4

`InputManager` warning lines after: **0** (`/tmp/striv-m10h-inputmanager-warnings-after.log` empty).

Checker result:
- Focused project: `Stride.Input`
- Build exit code: 0
- Focused warning count: 60
- Top focused warning codes: CS8618 (36), CS8602 (20), CS8625 (4)

`InputManager` is now warning-clean for focused warning extraction in this pass.

## 7) Deferred warnings
Remaining warnings (not targeted in M10h):
- SDL lifecycle/device/event: `Sources/SDL/*` (CS8618/CS8602/CS8625).
- RawInput lifecycle: none newly touched in this pass; leave for dedicated lane if surfaced.
- Gesture/event pipeline: multiple `Gestures/*` CS8602.
- Device/event DTO initialization: `Devices/*`, `Events/*` CS8618.
- Other: `InputEventPool.cs` CS8602/CS8625.

## 8) Validation results
- `dotnet test striv/tests/Stride.Input.Tests/Stride.Input.Tests.csproj -v minimal`
  - exit: 0
  - first meaningful warning/error: none
  - pass/fail: PASS
  - output truncated: no

- `dotnet build striv/projects/Stride.Input/Stride.Input.csproj -c Debug -p:StriVWarningFocusProject=Stride.Input --no-incremental`
  - exit: 0
  - first meaningful warning/error: first focused warning is CS8618 in `GamePadDeviceBase.cs`
  - pass/fail: PASS (with expected focused warnings)
  - output truncated: no

- `./striv/build/striv-check-focused-project.sh Stride.Input || true`
  - exit: 0 (script reported build exit code 0)
  - first meaningful warning/error: focused warnings remain (non-InputManager buckets)
  - pass/fail: PASS (checker functional)
  - output truncated: no

- `dotnet build striv/StriV.Core.slnx -c Debug -p:StriVWarningFocusProject=Stride.Input --no-incremental`
  - exit: 0
  - first meaningful warning/error: same focused Stride.Input warning set
  - pass/fail: PASS
  - output truncated: no

- `dotnet test striv/tests/StriV.AssetTool.Tests/StriV.AssetTool.Tests.csproj -v minimal`
  - exit: 0
  - first meaningful warning/error: none
  - pass/fail: PASS
  - output truncated: no

- `dotnet test striv/tests/StriV.AssetPipeline.Tests/StriV.AssetPipeline.Tests.csproj --no-build -v minimal`
  - exit: 0
  - first meaningful warning/error: none
  - pass/fail: PASS
  - output truncated: no

- `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj --no-build -v minimal`
  - exit: 0
  - first meaningful warning/error: one skipped test (`StreamLiveness_DoesNotPruneWhenAccessUnknown`)
  - pass/fail: PASS
  - output truncated: no

- `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj -v minimal || true`
  - command exit observed by harness chain: tolerated via `|| true`
  - underlying test result: FAIL for `FocusedWarningLane_BepuPhysics_HasZeroWarnings`
  - first meaningful warning/error: timeout after 30s in focused warning lane
  - pass/fail: FAIL (known test-harness timeout category; not Input behavior)
  - output truncated: no

- `./striv/build/striv-build-core.sh`
  - exit: 0
  - first meaningful warning/error: none
  - pass/fail: PASS
  - output truncated: no

## 9) Recommended next task
**Recommended next task: M10i Shine pass for SDL lifecycle warnings.**

Reasoning: with `InputManager` now warning-clean in focused extraction, the largest contiguous remaining bucket in `Stride.Input` is SDL lifecycle/device/event nullability warnings, making it the highest-value next reduction pass.
