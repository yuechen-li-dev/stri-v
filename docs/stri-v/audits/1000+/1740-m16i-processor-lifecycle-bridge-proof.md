# M16i Processor lifecycle bridge proof

## 1) Files changed
- Bridge:
  - `striv/projects/StriV.Engine.Dominatus/Events/ProcessorLifecycleEvents.cs`
  - `striv/projects/StriV.Engine.Dominatus/Actuators/IProcessorLifecycleActuator.cs`
  - `striv/projects/StriV.Engine.Dominatus/Transitions/ProcessorLifecycleTransition.cs`
  - `striv/projects/StriV.Engine.Dominatus/Nodes/ProcessorLifecycleNode.cs`
- Adapter:
  - `striv/projects/StriV.Engine.Dominatus.Adapters/Processors/StrideProcessorLifecycleActuator.cs`
- Tests:
  - `striv/tests/StriV.Engine.Dominatus.Tests/ProcessorLifecycleBridgeTests.cs`
  - `striv/tests/StriV.Engine.Dominatus.Tests/ProductionAdapterTests.cs`

`Stride.Engine` production files changed: **none**.

## 2) Task scope
Implemented processor lifecycle request -> actuator -> completed-event transitions and node helper surface only. No runtime rewiring, no `EntityManager` migration, no scheduler rewrite.

## 3) Current processor lifecycle API findings
- `EntityProcessor.OnSystemAdd()` and `EntityProcessor.OnSystemRemove()` are `protected internal abstract`.
- `EntityProcessor.ProcessEntityComponent(...)` is `protected internal abstract` and not callable cross-assembly from adapters.
- `EntityManager.Processors` is public and supports add/remove of processors, which drives existing manager lifecycle behavior.
- Tests can exercise protected/internal callbacks via real manager path (`Processors.Add/Remove`) or via recording/throwing actuator fakes.

## 4) Bridge additions
- Replaced old coarse processor events with explicit request/completed records for:
  - system add/remove
  - entity add/remove
- Extended `IProcessorLifecycleActuator` to four explicit methods.
- Added `ProcessorLifecycleTransition` static helper with argument guards and actuator exception propagation.
- Updated `ProcessorLifecycleNode` to provide request constructors and execute helpers.

## 5) Production adapter decision
A production adapter was added with mixed support:
- Supported safely:
  - `AddProcessorToSystemAsync` -> `entityManager.Processors.Add(processor)`
  - `RemoveProcessorFromSystemAsync` -> `entityManager.Processors.Remove(processor)`
- Explicitly blocked for entity-level add/remove:
  - Throws `NotSupportedException` because current API requires calling `ProcessEntityComponent`, which is not accessible cross-assembly and would otherwise require engine changes or runtime rewiring.

## 6) Tests
- `ProcessorLifecycleBridgeTests` proves request->transition->actuator->completed-event behavior, null-actuator guard, failure propagation, and node helper surface.
- `ProductionAdapterTests` proves system-level production adapter behavior and explicit entity-level not-supported behavior.
- All bridge tests use fakes/probes and do not rewire runtime scheduling.

## 7) Behavior compatibility
- No `Stride.Engine` behavior changes.
- No Dominatus dependency added to `Stride.Engine`.
- No runtime migration/rewiring performed.

## 8) Validation results
(abridged to first meaningful warnings/errors)
- `dotnet build striv/projects/StriV.Engine.Dominatus/StriV.Engine.Dominatus.csproj -c Debug -v minimal`
  - exit: 0, pass
  - first warning: legacy baseline warnings in `Stride.Core` (nullability/perf #warning)
  - truncated: yes
- `dotnet build striv/projects/StriV.Engine.Dominatus.Adapters/StriV.Engine.Dominatus.Adapters.csproj -c Debug -v minimal`
  - first run exit: 1, fail (expected during implementation)
  - first error: inaccessible `EntityProcessor.ProcessEntityComponent` from adapter assembly
  - truncated: yes
- same adapters build after fix
  - exit: 0, pass
  - warnings/errors: none
  - truncated: no
- `dotnet test striv/tests/StriV.Engine.Dominatus.Tests/StriV.Engine.Dominatus.Tests.csproj -v minimal`
  - exit: 0, pass (37 passed)
  - warnings/errors: none
  - truncated: no
- `dotnet build striv/projects/Stride.Engine/Stride.Engine.csproj -c Debug -p:StriVWarningFocusProject=Stride.Engine --no-incremental 2>&1 | tee /tmp/striv-m16i-engine-focused.log`
  - exit: 0, pass
  - first warning: existing nullability warning baseline in `Stride.Engine`
  - truncated: yes
- `dotnet build striv/StriV.Core.slnx -c Debug -v minimal`
  - exit: 0, pass
  - first warning: existing analyzer warning (`EnforceExtendedAnalyzerRules`) and existing test warnings
  - truncated: yes
- `./striv/build/striv-check-focused-projects.sh ...`
  - exit: 0, pass for all six focused projects
  - warnings/errors: none
  - truncated: no
- test suite command batch (reflection/games/input/cleangraph/asset tool/asset pipeline/shader pipeline)
  - exit: 0, pass
  - first warning: existing `Stride.Core.Reflection.Tests` obsolete warning baseline
  - truncated: yes
- `./striv/build/striv-build-core.sh`
  - exit: 0, pass
  - warnings/errors: none
  - truncated: yes

## 9) Recommended next task
**processor production adapter seam design if blocked**: add an explicit safe public processor-entity lifecycle seam in engine-side API (or a dedicated bridge service) so entity-level processor transitions can be actuated without protected/internal access or rewiring.
