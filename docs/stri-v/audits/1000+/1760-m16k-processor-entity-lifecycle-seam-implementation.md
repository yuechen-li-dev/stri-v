# M16k processor entity lifecycle seam implementation

## Files changed
- `striv/projects/Stride.Engine/Engine/EntityManager.cs`
- `striv/projects/StriV.Engine.Dominatus.Adapters/Processors/StrideProcessorLifecycleActuator.cs`
- `striv/tests/Stride.Engine.Tests/Stride.Engine.Tests.csproj`
- `striv/tests/Stride.Engine.Tests/ProcessorLifecycleInvokerTests.cs`
- `striv/tests/StriV.Engine.Dominatus.Tests/ProductionAdapterTests.cs`
- `striv/StriV.Core.slnx`

## Task scope
Implemented a minimal engine-owned seam on `EntityManager` for entity-addressed processor lifecycle invocation without Dominatus dependencies or runtime rewiring.

## Engine seam design
- Added:
  - `public void AddEntityToProcessor(EntityProcessor processor, Entity entity)`
  - `public void RemoveEntityFromProcessor(EntityProcessor processor, Entity entity)`
- Both validate null inputs and require `processor.EntityManager == this`.
- Add path reuses existing `CheckEntityWithNewProcessor` matching logic.
- Remove path forces `ProcessEntityComponent(..., forceRemove: true)` only for accepted component types on the entity.
- Semantics are documented as entity-addressed but component-match-driven.

## Engine tests
- Added focused tests in `Stride.Engine.Tests` for:
  - add invokes once for matching entity;
  - remove invokes once after add;
  - non-matching entity does not add;
  - add/remove reject unbound processor with deterministic `InvalidOperationException`.
- Result: green.

## Adapter changes
- `StrideProcessorLifecycleActuator` now resolves manager from `processor.EntityManager` and calls new seam methods.
- Entity-level operations are now supported for bound processors.

## Dominatus transition tests
- Updated production adapter tests to prove:
  - actuator add/remove entity uses seam and triggers callbacks.
  - transition add/remove returns completed events through production adapter.

## Behavior compatibility
- Existing processor matching/order behavior preserved by routing through manager-owned logic.
- No direct Dominatus reference added to `Stride.Engine`.
- No processor/scheduler rewiring.
- Warning cleanup not targeted.

## Validation results
- `dotnet test striv/tests/Stride.Engine.Tests/Stride.Engine.Tests.csproj -v minimal` exit 0, pass.
- `dotnet build striv/projects/StriV.Engine.Dominatus/StriV.Engine.Dominatus.csproj -c Debug -v minimal` exit 0, pass.
- `dotnet build striv/projects/StriV.Engine.Dominatus.Adapters/StriV.Engine.Dominatus.Adapters.csproj -c Debug -v minimal` exit 0, pass.
- `dotnet test striv/tests/StriV.Engine.Dominatus.Tests/StriV.Engine.Dominatus.Tests.csproj -v minimal` exit 0, pass.
- `dotnet build striv/projects/Stride.Engine/Stride.Engine.csproj -c Debug -p:StriVWarningFocusProject=Stride.Engine --no-incremental` exit 0, pass, warnings present (existing baseline).

## Recommended next task
Opt-in integration test composing transform + scene + processor transitions in one end-to-end lifecycle flow.
