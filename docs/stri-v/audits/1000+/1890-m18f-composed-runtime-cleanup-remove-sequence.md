# M18f — Composed runtime cleanup/remove sequence

## 1) Files changed
- `striv/projects/StriV.Engine.Dominatus/Runtime/ProcessorLifecycleActuation.cs`
- `striv/projects/StriV.Engine.Dominatus/Nodes/ProcessorLifecycleDominatusNodes.cs`
- `striv/projects/StriV.Engine.Dominatus/Nodes/EngineLifecycleDominatusNodes.cs`
- `striv/tests/StriV.Engine.Dominatus.Tests/Runtime/EngineLifecycleRuntimeTests.cs`
- `docs/stri-v/audits/1000+/1890-m18f-composed-runtime-cleanup-remove-sequence.md`

Production `Stride.Engine` files changed: **No**.

## 2) Task scope
Added a small composed runtime cleanup/remove proof using real Dominatus runtime `Ai.Act(...)` commands and production adapters, without runtime rewiring.

## 3) Node design
- Existing composed add node retained.
- Added composed add+cleanup node sequence:
  1. `EntitySceneAttachRequested(parent, scene)`
  2. `EntitySceneAttachRequested(child, scene)`
  3. `TransformParentAttachRequested(child, parent)`
  4. `ProcessorSystemAddRequested(processor, entityManager)`
  5. `ProcessorEntityAddRequested(processor, child)`
  6. `ProcessorEntityRemoveRequested(processor, child)`
  7. `ProcessorSystemRemoveRequested(processor, entityManager)`
- No scene/transform detach cleanup was added.

## 4) Runtime handlers
Added processor remove actuation handlers:
- `ProcessorEntityRemoveActuationHandler`
- `ProcessorSystemRemoveActuationHandler`

Used existing runtime handlers for add/scene/transform attach.

## 5) Tests
Added composed runtime test:
- `DominatusRuntime_ComposedLifecycle_AddThenProcessorCleanup_ComposesThroughSampleStyleNode`

Runtime path:
- Real `AiWorld` + `AiAgent` via M18e harness.
- Production adapters: scene, transform, processor lifecycle adapters.

Assertions:
- Scene attach and transform parent attach remain in place.
- Processor add callback count is 1.
- Processor remove callback count is 1.
- Removed entity is child.
- Processor detached from manager and removed from processor collection.

Completion behavior:
- Node completes through registered `IActuationHandler<T>` chain; remove payloads are returned only after transition+adapter success.

## 6) Behavior compatibility
- No engine behavior changed.
- No direct Dominatus dependency added to `Stride.Engine`.
- Adapter path remains opt-in.

## 7) Runtime harness observation
M18e harness remains sufficient for this scope; no further extraction needed for M18f.

## 8) Validation results
- `dotnet build striv/projects/StriV.Engine.Dominatus/StriV.Engine.Dominatus.csproj -c Debug -v minimal`
  - exit: 0
  - first meaningful warning/error: existing solution warnings (non-M18f)
  - pass/fail: pass
  - output truncated: yes
- `dotnet build striv/projects/StriV.Engine.Dominatus.Adapters/StriV.Engine.Dominatus.Adapters.csproj -c Debug -v minimal`
  - exit: 0
  - first meaningful warning/error: none
  - pass/fail: pass
  - output truncated: no
- `dotnet test striv/tests/StriV.Engine.Dominatus.Tests/StriV.Engine.Dominatus.Tests.csproj -v minimal`
  - exit: 0
  - first meaningful warning/error: none
  - pass/fail: pass
  - output truncated: no
- `dotnet build striv/projects/Stride.Engine/Stride.Engine.csproj -c Debug -p:StriVWarningFocusProject=Stride.Engine --no-incremental 2>&1 | tee /tmp/striv-m18f-engine-focused.log`
  - exit: 0
  - first meaningful warning/error: existing `Stride.Engine` nullability warnings
  - pass/fail: pass
  - output truncated: yes
- `dotnet build striv/StriV.Core.slnx -c Debug -v minimal`
  - exit: 0
  - first meaningful warning/error: existing solution warnings
  - pass/fail: pass
  - output truncated: yes
- `./striv/build/striv-check-focused-projects.sh Stride.BepuPhysics Stride.Core.Mathematics Stride.Core.IO Stride.Input Stride.Games Stride.Core.Reflection`
  - exit: 0
  - first meaningful warning/error: none
  - pass/fail: pass
  - output truncated: no
- Combined sequential test command (nine `dotnet test` invocations from task spec)
  - exit: 0
  - first meaningful warning/error: one expected skip in shader pipeline tests
  - pass/fail: pass
  - output truncated: yes
- `./striv/build/striv-build-core.sh`
  - exit: 0
  - first meaningful warning/error: none
  - pass/fail: pass
  - output truncated: yes

## 9) Recommendation
Proceed with the first runtime opt-in design: use composed Dominatus nodes for add and cleanup/remove steps, with processor cleanup as the minimal production migration candidate. Add scene/transform detach actuation only when a concrete motivating runtime path requires it.
