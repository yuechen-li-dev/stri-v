# 1840 - M18a transform Ai.Act runtime prototype

## 1) Files changed
- `striv/projects/StriV.Engine.Dominatus/Events/TransformLifecycleEvents.cs`
- `striv/projects/StriV.Engine.Dominatus/Nodes/TransformLifecycleDominatusNodes.cs`
- `striv/projects/StriV.Engine.Dominatus/Runtime/TransformLifecycleActuation.cs`
- `striv/tests/StriV.Engine.Dominatus.Tests/Runtime/TransformLifecycleRuntimeTests.cs`

`Stride.Engine` production files changed: **none**.

## 2) Task scope
This milestone implements the first runtime opt-in prototype for transform parent attach only. The node uses `Ai.Act(...)` with a Dominatus command payload and is executed in real Dominatus `AiWorld` + `HfsmInstance` runtime in tests. No default engine runtime rewiring was introduced.

## 3) Dominatus API findings
- Node authoring uses `IEnumerator<AiStep>` and `yield return Ai.Act(IActuationCommand, ...)`.
- Runtime execution path is `AiWorld` + `AiAgent` + `HfsmInstance`, and driving via `world.Tick(dt)`.
- Actuation dispatch/handling goes through `ActuatorHost.Register(IActuationHandler<TCmd>)` and `ActuatorHost.Dispatch`.
- Completion can be immediate with `HandlerResult.Completed...` or deferred via `CompleteLater`.
- `Ai.Await(...)` is optional pairing for in-flight completion waits; not required for immediate completion this prototype uses.
- Minimal setup is: graph/root state, actuator host + handler registration, world/agent add, initialize brain, tick.

## 4) Runtime prototype design
- Node shape: one-step node that emits `Ai.Act(new TransformParentAttachRequested(child, parent))`.
- Actuation payload: reused existing `TransformParentAttachRequested` as the command by implementing `IActuationCommand` on that record.
- Handler mapping: `TransformParentAttachActuationHandler` maps command to `TransformLifecycleTransition.AttachParentAsync(...)`.
- Production adapter usage: handler injects and calls `StrideTransformLifecycleActuator` (no fake adapter on primary proof).

## 5) Tests
- `DominatusRuntime_AttachTransformParent_ActsThroughProductionAdapter`
  - Runtime path: Dominatus `HfsmInstance` in `AiWorld` tick.
  - Assertions: child parent set to parent transform, parent children contains child transform.
  - Adapter: `StrideTransformLifecycleActuator` through runtime handler.
  - Completion behavior: immediate completion via handler `CompletedWithPayload`.

## 6) Behavior compatibility
- No engine runtime behavior changed.
- No direct Dominatus dependency was added to `Stride.Engine`.
- Adapter path remains opt-in and test-wired.

## 7) Validation results
- `dotnet build striv/projects/StriV.Engine.Dominatus/StriV.Engine.Dominatus.csproj -c Debug -v minimal`
  - Exit code: 0
  - First meaningful warning/error: existing Stride warning noise in dependencies.
  - Pass/fail: pass
  - Output truncated: yes
- `dotnet build striv/projects/StriV.Engine.Dominatus.Adapters/StriV.Engine.Dominatus.Adapters.csproj -c Debug -v minimal`
  - Exit code: 0
  - First meaningful warning/error: none
  - Pass/fail: pass
  - Output truncated: no
- `dotnet test striv/tests/StriV.Engine.Dominatus.Tests/StriV.Engine.Dominatus.Tests.csproj -v minimal`
  - Exit code: 0
  - First meaningful warning/error: none
  - Pass/fail: pass
  - Output truncated: no
- `dotnet build striv/projects/Stride.Engine/Stride.Engine.csproj -c Debug -p:StriVWarningFocusProject=Stride.Engine --no-incremental 2>&1 | tee /tmp/striv-m18a-engine-focused.log`
  - Exit code: 0
  - First meaningful warning/error: existing nullability warnings in `Stride.Engine`.
  - Pass/fail: pass
  - Output truncated: yes

## 8) Lessons / next task
Recommended next task: M18b scene attach runtime `Ai.Act` prototype, then M18b composed transform+scene runtime prototype once scene actuation node/handler is validated with same opt-in pattern.
