# 1940 — M20a full-cycle entity lifecycle runner

## 1) Files changed
- `striv/projects/StriV.Engine.Dominatus/Nodes/EngineLifecycleDominatusNodes.cs`
- `striv/projects/StriV.Engine.Dominatus.Runtime/StriVEngineLifecycleRunner.cs`
- `striv/tests/StriV.Engine.Dominatus.Tests/Runtime/StriVEngineLifecycleRunnerTests.cs`
- `docs/stri-v/audits/1000+/1940-m20a-full-cycle-entity-lifecycle-runner.md`

`Stride.Engine` production source files changed: **none**.

## 2) Task scope
M20a adds one opt-in, host-facing lifecycle runner method that performs one bounded full cycle across scene/transform/processor lifecycle surfaces. This composes existing proven attach and cleanup primitives without rewiring default engine runtime behavior.

## 3) Node design
Added one sample-style Dominatus node using immediate `Ai.Act(...)` steps only:
1. scene attach parent
2. scene attach child
3. transform parent attach child -> parent
4. processor system add
5. processor entity add
6. processor entity remove
7. processor system remove
8. transform parent detach child
9. scene detach child
10. scene detach parent

No adapter calls and no direct Stride mutation in node body.

## 4) Runner design
Added public method:
- `RunSceneTransformProcessorFullCycleAsync(Scene, Entity, Entity, EntityManager, EntityProcessor, CancellationToken)`

Behavior:
- null argument validation
- cancellation-before-start check
- registers required handlers/adapters for scene attach/detach, transform attach/detach, processor add/remove (system/entity)
- executes the full-cycle node through existing private `RunSingleNodeAsync(...)`
- reuses M19c bounded tick behavior and cancellation-between-ticks behavior
- no generic framework introduced

## 5) Tests
Added tests:
- `StriVEngineLifecycleRunner_RunSceneTransformProcessorFullCycle_RunsThroughDominatusRuntime`
  - setup: scene, parent/child entities, child test component, scene instance entity manager, recording processor
  - exercise: `RunSceneTransformProcessorFullCycleAsync(...)`
  - asserts final cleanup state for scene membership, transform parentage, processor/system detachment, and callback counts/entities
- `StriVEngineLifecycleRunner_RunSceneTransformProcessorFullCycle_RejectsNullArguments`
  - validates null guarding on all required parameters

## 6) Behavior compatibility
- default engine behavior unchanged
- adapters remain opt-in via runner construction and invocation
- no direct Dominatus dependency added to `Stride.Engine`

## 7) Validation results
Commands run in sequence; output below captures first meaningful warning/error signal and truncation state.

1. `dotnet build striv/projects/StriV.Engine.Dominatus.Runtime/StriV.Engine.Dominatus.Runtime.csproj -c Debug -v minimal`
   - exit code: 0
   - first meaningful warning/error: `warning CS1030: #warning: 'PERF: Do not copy byte-for-byte.'`
   - pass/fail: pass
   - output truncated: yes

2. `dotnet test striv/tests/StriV.Engine.Dominatus.Tests/StriV.Engine.Dominatus.Tests.csproj -v minimal`
   - exit code: 0
   - first meaningful warning/error: none (tests passed)
   - pass/fail: pass
   - output truncated: no

3. `dotnet build striv/projects/Stride.Engine/Stride.Engine.csproj -c Debug -p:StriVWarningFocusProject=Stride.Engine --no-incremental 2>&1 | tee /tmp/striv-m20a-engine-focused.log`
   - exit code: 0
   - first meaningful warning/error: `warning CS8765: Nullability of type of parameter 'obj' doesn't match overridden member`
   - pass/fail: pass
   - output truncated: yes

4. `dotnet build striv/StriV.Core.slnx -c Debug -v minimal`
   - exit code: 0
   - first meaningful warning/error: `warning CS1030: #warning: 'PERF: Do not copy byte-for-byte.'`
   - pass/fail: pass
   - output truncated: yes

## 8) Recommended next task
Recommended: **M20b entity lifecycle orchestrator interface / opt-in service**.
Reason: this keeps callsites clean while preserving opt-in semantics and avoiding runtime rewiring.
