# 1830 — M17e EntityCloner operation lifecycle proof

## 1) Files changed
- `striv/projects/StriV.Engine.Dominatus/Events/EntityCloneEvents.cs`
- `striv/projects/StriV.Engine.Dominatus/Actuators/IEntityCloneActuator.cs`
- `striv/projects/StriV.Engine.Dominatus/Transitions/EntityCloneTransition.cs`
- `striv/projects/StriV.Engine.Dominatus/Nodes/EntityCloneNode.cs`
- `striv/projects/StriV.Engine.Dominatus.Adapters/Cloning/StrideEntityCloneActuator.cs`
- `striv/tests/StriV.Engine.Dominatus.Tests/EntityCloneLifecycleTests.cs`

Stride.Engine production files changed: **none**.

## 2) Task scope
This slice proves a bounded operation lifecycle for clone intent through Dominatus bridge surfaces only:
`EntityCloneRequested -> actuator -> EntityCloneCompleted`, with exception propagation on actuator failure.

No runtime rewiring, no direct Dominatus dependency from Stride.Engine, and no broad serializer/sourcegen work were introduced.

## 3) Current EntityCloner behavior map
- Public APIs: `EntityCloner.Clone(Entity)` and `EntityCloner.Instantiate(Prefab)`.
- Inputs: non-null `Entity`/`Prefab`; clone internals gather entity/component graph recursively.
- Behavior: deep clones entity tree/components, shares external assets via clone serialization profile.
- Clone context cleanup: `CloneContext.Cleanup()` resets memory stream + mapped/shared/serialized references.
- Scene/manager membership: clone API does not explicitly attach clones to a scene or manager.
- Deterministic proof surface used here: bridge lifecycle semantics and payload correctness are testable without full runtime bootstrapping.
- Deferred: full serializer-runtime initialization path needed for deterministic direct `EntityCloner.Clone` success in isolated test host.

## 4) Bridge additions
- Events/messages: `EntityCloneRequested`, `EntityCloneCompleted`.
- Actuator interface: `IEntityCloneActuator.CloneEntityAsync`.
- Transition helper: `EntityCloneTransition.CloneEntityAsync` with null checks and null clone rejection.
- Node helper: `EntityCloneNode` request/execute surface.

## 5) Adapter behavior
`StrideEntityCloneActuator` is a thin adapter over `EntityCloner.Clone(source)` with argument null-guard and null return guard.
No custom clone algorithm was added.

## 6) Tests
- Lifecycle success: transition executes actuator and emits completed event with correct source + clone properties.
- Failure propagation: actuator exception propagates (no fake completed event).
- Null actuator rejection: throws `ArgumentNullException`.
- Node surface: request and execute helpers expose clone intent and lifecycle path.
- Adapter surface test: verifies adapter calls current API path (observed isolated-host serializer exception is propagated).

## 7) Behavior compatibility
- No engine runtime behavior changed.
- No Dominatus dependency added into Stride.Engine.
- Adapter remains opt-in and external to engine runtime wiring.

## 8) Validation results
1. `dotnet build striv/projects/StriV.Engine.Dominatus/StriV.Engine.Dominatus.csproj -c Debug -v minimal`
   - exit: 0
   - first warning/error: none
   - pass/fail: pass
   - truncated: no
2. `dotnet build striv/projects/StriV.Engine.Dominatus.Adapters/StriV.Engine.Dominatus.Adapters.csproj -c Debug -v minimal`
   - exit: 0
   - first warning/error: none
   - pass/fail: pass
   - truncated: no
3. `dotnet test striv/tests/StriV.Engine.Dominatus.Tests/StriV.Engine.Dominatus.Tests.csproj -v minimal`
   - exit: 0
   - first warning/error: none
   - pass/fail: pass
   - truncated: no
4. `dotnet build striv/projects/Stride.Engine/Stride.Engine.csproj -c Debug -p:StriVWarningFocusProject=Stride.Engine --no-incremental 2>&1 | tee /tmp/striv-m17e-engine-focused.log`
   - exit: 0
   - first warning/error: `CS8765` nullability warnings (legacy baseline)
   - pass/fail: pass (warnings expected)
   - truncated: yes (console transcript)
5. `dotnet build striv/StriV.Core.slnx -c Debug -v minimal`
   - exit: 0
   - first warning/error: `CS1030` perf warning in legacy assembly processor source mirror
   - pass/fail: pass (warnings expected)
   - truncated: yes (console transcript)

## 9) Recommended next task
Move from proof-only slices into active implementation:
1. runtime opt-in prototype for clone lifecycle runner wiring through existing adapter boundaries;
2. first real Dominatus-backed lifecycle runner test in a runtime harness;
3. one small production migration using current adapters, with rollback-safe opt-in guard.
