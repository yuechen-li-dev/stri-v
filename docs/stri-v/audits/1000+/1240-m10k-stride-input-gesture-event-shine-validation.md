# 1240 — M10k Stride.Input gesture/event Shine validation

## 1) Files changed
- striv/projects/Stride.Input/InputEventPool.cs
- striv/projects/Stride.Input/Gestures/GestureRecognizer.cs
- striv/projects/Stride.Input/Gestures/GestureRecognizerDrag.cs
- striv/projects/Stride.Input/Gestures/GestureRecognizerFlick.cs
- striv/projects/Stride.Input/Gestures/GestureRecognizerLongPress.cs
- striv/projects/Stride.Input/Gestures/GestureRecognizerTap.cs
- striv/projects/Stride.Input/Gestures/GestureRecognizerComposite.cs
- striv/projects/Stride.Input/Properties/AssemblyInfo.cs
- striv/tests/Stride.Input.Tests/GestureEventPipelineTests.cs

## 2) Task scope
M10k focused on gesture/event pipeline and InputEventPool warnings only. SDL/InputManager remained untouched. Broader device/event DTO initialization warnings remain deferred.

## 3) Before warnings
- Focused warning count before: 46.
- Distribution before: CS8618=26, CS8602=18, CS8625=2.
- Gesture/event/InputEventPool warning lines before (grep bucket): 30 (contains duplicated compile summary lines), unique sites include:
  - InputEventPool.cs CS8602/CS8625
  - Gestures/* recognizers CS8602
  - Events/* DTO CS8618
- Focused checker status before: 4.

## 4) Gesture/event pipeline map
- `InputEventPool<T>` keeps per-thread pools via `ThreadLocal<Pool>` and recycles events by nulling `Device` then removing from `PoolListStruct`.
- Gesture recognizers aggregate `PointerEvent` frames, stage events in `CurrentGestureEvents`, and publish into output list each frame.
- Tap recognizer only emits on tap finalization (`EndCurrentTap`), not immediately after simple press/release pairs.
- Nullability warnings were mostly from `as` casts on pool-backed event add operations and `ThreadLocal.Value` flow.

## 5) Tests added
- `InputEventPool_GetOrCreate_ThenEnqueue_ClearsDeviceAndReusesInstance`
  - Locks that pooled event instances are reusable through dequeue/enqueue cycle.
  - Supports InputEventPool null-flow cleanup.
  - Deterministic: no timing/system dependencies.
- `GestureRecognizerTap_TwoTapFrames_DoNotEmitWithoutTimeoutFlush`
  - Locks current behavior: two quick tap frames do not immediately emit tap event without lifecycle flush.
  - Supports safe recognizer warning fixes without semantic change.
  - Deterministic fixed input frames.

## 6) Fixes applied
- `InputEventPool.cs`
  - Addressed CS8602/CS8625 with explicit `ThreadLocal.Value!` usage and lifecycle-safe `null!` reset on enqueue.
- Gesture recognizers
  - Replaced nullable `as` casts from pool allocations with direct typed casts where factory guarantees event subtype.
  - Added `ThreadLocal.Value!` in base recognizer cache usage.
- `AssemblyInfo.cs`
  - Added `InternalsVisibleTo("Stride.Input.Tests")` to enable focused internal gesture tests.

## 7) After warnings
- Focused warning count after: 26.
- Distribution after: CS8618=26.
- Gesture/event/InputEventPool warning lines after: 10 (all CS8618 DTO initialization style warnings under Events and related types; no Gesture/InputEventPool CS8602/CS8625 left).
- Focused checker status after: 4.

## 8) Deferred warnings
- Device/event DTO initialization bucket remains (CS8618 across InputEvent/DeviceChanged/TextInput/InputPreUpdate and device types).
- Simulated source bucket remains (`GamePadSimulated.cs` CS8618).
- No new SDL/InputManager/RawInput work in this pass.

## 9) Validation results
Commands run with observed exit codes:
- `dotnet build striv/projects/Stride.Input/Stride.Input.csproj -c Debug -p:StriVWarningFocusProject=Stride.Input --no-incremental` => 0 (warnings present).
- `./striv/build/striv-check-focused-project.sh Stride.Input` => 4 (focused warnings remain).
- `dotnet test striv/tests/Stride.Input.Tests/Stride.Input.Tests.csproj -v minimal` => 0.
- `dotnet build striv/StriV.Core.slnx -c Debug -p:StriVWarningFocusProject=Stride.Input --no-incremental` => 0.
- `dotnet test striv/tests/StriV.AssetTool.Tests/StriV.AssetTool.Tests.csproj -v minimal` => 0.
- `dotnet test striv/tests/StriV.AssetPipeline.Tests/StriV.AssetPipeline.Tests.csproj --no-build -v minimal` => 0.
- `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj --no-build -v minimal` => 0.
- `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj -v minimal` => 0.
- `./striv/build/striv-build-core.sh` => 0.

## 10) Recommended next task
M10l device/event DTO initialization cleanup (CS8618 bucket) is the highest value next step, since gesture/event-pool null-flow bucket was reduced in this pass.
