# 1850 — M18b scene `Ai.Act(...)` runtime prototype

## 1) Files changed
- `striv/projects/StriV.Engine.Dominatus/Events/SceneLifecycleEvents.cs`
- `striv/projects/StriV.Engine.Dominatus/Runtime/SceneLifecycleActuation.cs` (new)
- `striv/projects/StriV.Engine.Dominatus/Nodes/SceneLifecycleDominatusNodes.cs` (new)
- `striv/tests/StriV.Engine.Dominatus.Tests/Runtime/SceneLifecycleRuntimeTests.cs` (new)

`Stride.Engine` production files changed: **none**.

## 2) Task scope
Implemented runtime opt-in scene attach via Dominatus `Ai.Act(...)`, mirroring M18a transform-path architecture, plus optional composed scene+transform runtime test. No runtime rewiring performed.

## 3) Runtime design
- Node shape: `AttachEntityToScene(Entity, Scene)` yields `Ai.Act(new EntitySceneAttachRequested(...))`.
- Command payload: `EntitySceneAttachRequested` now implements `IActuationCommand`.
- Handler mapping: `EntitySceneAttachActuationHandler : IActuationHandler<EntitySceneAttachRequested>` calls transition helper.
- Production adapter usage: handler -> `SceneLifecycleTransition.AttachEntityAsync(...)` -> `ISceneLifecycleActuator` -> `StrideSceneLifecycleActuator`.
- Runtime path: `ActuatorHost.Register(...)`, `AiWorld`, `AiAgent`, `HfsmInstance`, `world.Tick(dt)`.

## 4) Tests
- `DominatusRuntime_AttachEntityToScene_ActsThroughProductionAdapter`
  - Runtime path: real Dominatus runtime (`AiWorld`/`AiAgent`/`HfsmInstance`) + tick.
  - Adapter: `StrideSceneLifecycleActuator`.
  - Assertions: `entity.Scene == scene`, `scene.Entities` contains entity.
  - Completion: immediate completed actuation via `CompletedWithPayload`.

- `DominatusRuntime_SceneThenTransformAttach_ComposesThroughProductionAdapters`
  - Runtime path: same real runtime path.
  - Adapters: `StrideSceneLifecycleActuator` + `StrideTransformLifecycleActuator`.
  - Assertions: scene membership for parent + transform parent/child linkage for child.
  - Completion: immediate completed actuation chain in node order.

## 5) Ordering doctrine
Composed test preserves required order:
1. scene attach parent;
2. scene attach child;
3. transform parent attach child->parent.

## 6) Behavior compatibility
- No engine behavior changed.
- No direct Dominatus dependency added to `Stride.Engine`.
- Adapter path remains runtime opt-in.

## 7) Validation results
1. `dotnet build striv/projects/StriV.Engine.Dominatus/StriV.Engine.Dominatus.csproj -c Debug -v minimal`
   - Exit: 0
   - First meaningful warning/error: legacy warning flood in dependent Stride projects (expected baseline).
   - Pass/Fail: Pass
   - Output truncated: Yes

2. `dotnet build striv/projects/StriV.Engine.Dominatus.Adapters/StriV.Engine.Dominatus.Adapters.csproj -c Debug -v minimal`
   - Exit: 0
   - First meaningful warning/error: none
   - Pass/Fail: Pass
   - Output truncated: No

3. `dotnet test striv/tests/StriV.Engine.Dominatus.Tests/StriV.Engine.Dominatus.Tests.csproj -v minimal`
   - Exit: 0 (after fixing compile and assertion issue)
   - First meaningful warning/error: initially compile errors from namespace qualification and one failing assertion in composed test; resolved.
   - Pass/Fail: Pass
   - Output truncated: No

4. `dotnet build striv/projects/Stride.Engine/Stride.Engine.csproj -c Debug -p:StriVWarningFocusProject=Stride.Engine --no-incremental 2>&1 | tee /tmp/striv-m18b-engine-focused.log`
   - Exit: 0
   - First meaningful warning/error: existing Stride.Engine warning baseline
   - Pass/Fail: Pass
   - Output truncated: Yes

5. `dotnet build striv/StriV.Core.slnx -c Debug -v minimal`
   - Exit: 0
   - First meaningful warning/error: baseline warnings in assembly processor/test projects
   - Pass/Fail: Pass
   - Output truncated: Yes

## 8) Recommendation
Proceed to **M18c processor runtime `Ai.Act` prototype** (or harness abstraction if desired), since scene and transform composition now validates the scene-before-transform ordering in the real Dominatus runtime path.
