# 1750 — M16j Processor Entity Lifecycle Seam Design

## 1) Files changed

- `docs/stri-v/audits/1000+/1750-m16j-processor-entity-lifecycle-seam-design.md` (this report only).

`Stride.Engine` production files changed: **No**.

## 2) Task scope

This task is design/audit-first and intentionally avoids implementation changes to runtime engine lifecycle behavior.

Scope handled:

- Determine ownership and ordering of processor lifecycle in current Stride engine internals.
- Identify the minimal safe seam for Dominatus adapters to actuate entity-level processor lifecycle.
- Avoid reflection, private/protected bypasses, and runtime rewiring.
- Keep `Stride.Engine` free of Dominatus-specific dependency.

Out of scope in M16j:

- Implementing new engine runtime behavior.
- Rewiring `EntityManager`/processor flow.
- Using `InternalsVisibleTo` as a workaround.

## 3) Current lifecycle findings

### Callback visibility and call ownership

- `EntityProcessor.ProcessEntityComponent(Entity, EntityComponent, bool)` is `protected internal abstract` on `EntityProcessor`.
- Concrete entity membership bookkeeping happens in `EntityProcessor<TComponent, TData>.ProcessEntityComponent(...)`, including:
  - match evaluation,
  - add/remove transitions,
  - `ComponentDatas` map mutation,
  - `OnEntityComponentAdding` / `OnEntityComponentRemoved` callback dispatch.

### Who decides membership/matching

- `EntityManager` drives membership updates and dispatch:
  - `CheckEntityWithProcessors(...)`
  - `CheckEntityComponentWithProcessors(...)`
  - `CheckEntityWithNewProcessor(...)`
  - `UpdateDependentProcessors(...)`
- Matching itself is resolved inside `EntityProcessor<TComponent, TData>.EntityMatch(...)`, combining main component type + required component types.

### Who calls `ProcessEntityComponent` today

Only engine-owned flows in `EntityManager` call it directly, across add/remove/component-change/system-processor-add paths.

### Ordering guarantees observed

- On entity add: entity is attached to manager first, then processor checks, then pending processors registered, then `OnEntityAdded` event.
- On entity remove: processor checks with `forceRemove=true` occur before entity manager detaches and before `OnEntityRemoved` event.
- On component changes: old-component removal path first, new-component add path second, dependent processors refreshed afterwards.
- On processor add: processor is registered (`EntityManager`, `Services`, `OnSystemAdd`) before scanning existing entities.

### Why adapters are currently blocked

- Adapter assembly cannot safely call `ProcessEntityComponent(...)` due to accessibility and ownership constraints.
- Production Dominatus adapter intentionally throws `NotSupportedException` for entity-level methods to avoid unsafe bypass.

### Existing indirect public API

- System-level processor lifecycle is publicly actuated via `entityManager.Processors.Add/Remove(processor)`.
- No public or adapter-safe API currently exposes entity-level processor membership actuation.

### Existing test seam

- Dominatus tests currently validate:
  - system processor add/remove via production adapter,
  - entity-level methods are explicitly unsupported (throwing).

## 4) Semantics map

```text
Entity added to manager
  -> entity attached to manager
  -> for each entity component:
       processor list resolution by component type
       ProcessEntityComponent(..., forceRemove=false)
       processor-internal match check + add/remove decision
  -> pending default processors can be auto-collected and added
  -> EntityAdded event

Entity removed from manager
  -> for each entity component:
       ProcessEntityComponent(..., forceRemove=true)
  -> entity detached from manager
  -> EntityRemoved event

Component added/removed (NotifyComponentChanged)
  -> optional default processor collection for new component type
  -> old component ProcessEntityComponent(..., forceRemove=true)
  -> new component ProcessEntityComponent(..., forceRemove=false)
  -> dependent processors revalidated via UpdateDependentProcessors
  -> ComponentChanged event
```

Conclusion: this lifecycle is fundamentally **component-match driven** rather than pure entity-list add/remove.

Naming fit note:

- Current Dominatus event names (`ProcessorEntityAddRequested/Removed`) are serviceable at high level, but technically they represent “processor membership transition induced by component matching”.
- Optional future rename candidate: `ProcessorMatchAdded/Removed` or `ProcessorEntityMatchAddRequested/Removed`.

## 5) Seam options

| Option | Description | Feasibility | Risk | Tests needed | Recommendation |
| ------ | ----------- | ----------- | ---- | ------------ | -------------- |
| A | Public lifecycle invoker interface in `Stride.Engine` | Medium | Medium (risk of wrong abstraction if entity-only API hides component semantics) | Engine unit tests for match correctness and ordering; Dominatus adapter integration tests | Viable only if invoker delegates to manager-owned flow and accepts component context or derives it safely |
| B | Static helper in `Stride.Engine` | Medium | High (easy to bypass manager-owned ordering/context; utility creep) | Same as A + additional invariants around manager preconditions | Not recommended |
| C | Narrow method on real owner (`EntityManager` preferred) | High | Low-Medium (touches load-bearing manager but preserves ordering in one place) | Focused engine tests + adapter tests; no behavior drift tests | **Recommended** |
| D | `InternalsVisibleTo` bridge | Low architectural quality | High (broad exposure, brittle boundary, still awkward with protected abstract API) | Hard to constrain; ongoing maintenance burden | Not recommended except emergency fallback |
| E | Defer seam entirely | High | Low immediate, high roadmap blocking | Existing tests remain | Reasonable only if M16k is immediately scheduled |

## 6) Recommended seam

### Recommendation: Option C (owner-scoped method on `EntityManager`)

Smallest safe seam is to expose a **narrow, engine-owned method on `EntityManager`** that invokes existing manager-controlled membership flow rather than exposing processor internals.

Proposed shape (illustrative):

```csharp
public void ActuateProcessorEntityLifecycle(EntityProcessor processor, Entity entity, bool add)
```

or two explicit methods:

```csharp
public void AddEntityToProcessor(EntityProcessor processor, Entity entity)
public void RemoveEntityFromProcessor(EntityProcessor processor, Entity entity)
```

Key design constraints for this seam:

1. Enforce preconditions (`processor`, `entity`, and `processor.EntityManager == this` where required).
2. Route through existing `CheckEntityComponentWithProcessors`/`ProcessEntityComponent` paths and current ordering logic.
3. Do not expose raw `ProcessEntityComponent` or processor internal dictionaries publicly.
4. Keep API generic to engine use-cases; no Dominatus references.

Why this is smallest/safest:

- ownership remains with `EntityManager`, where lifecycle ordering already exists;
- no reflection/private access or runtime rewiring;
- preserves component-match semantics and required-type dependency updates;
- adapter can call a legitimate engine seam later.

## 7) Test plan (for M16k implementation)

Engine-level tests (new/extended in engine test project):

1. `ProcessorLifecycleInvoker_AddEntity_InvokesOnEntityComponentAddingOnce`
2. `ProcessorLifecycleInvoker_RemoveEntity_InvokesOnEntityComponentRemovedOnce`
3. `ProcessorLifecycleInvoker_RespectsProcessorComponentMatching`
4. `ProcessorLifecycleInvoker_RespectsRequiredTypesDependency`
5. `ProcessorLifecycleInvoker_RequiresProcessorBoundToSameEntityManager`
6. `ProcessorLifecycleInvoker_PreservesComponentChangeOrdering`

Dominatus-side tests:

7. `StrideProcessorLifecycleActuator_AddEntity_UsesEngineSeam`
8. `StrideProcessorLifecycleActuator_RemoveEntity_UsesEngineSeam`
9. `ProcessorLifecycleTransition_AddEntity_ThroughProductionAdapter_ReturnsCompletedEvent`
10. `ProcessorLifecycleTransition_RemoveEntity_ThroughProductionAdapter_ReturnsCompletedEvent`

Probe processor shape:

- Use small recording processor derived from `EntityProcessor<TComponent>` or `EntityProcessor<TComponent, TData>` to count add/remove callbacks and capture ordering markers.

## 8) Prototype result

No prototype implementation performed in M16j.

Reason deferred to M16k:

- No existing public API currently provides exact safe entity-level actuation.
- Adding a new `EntityManager` seam is a runtime API change and should land with focused engine tests in a dedicated implementation task.

## 9) Behavior compatibility

- Runtime behavior changed in M16j: **No**.
- Direct Dominatus dependency added to `Stride.Engine`: **No**.

## 10) Validation results

| Command | Exit code | First meaningful warning/error | Pass/Fail | Output truncated |
| --- | ---: | --- | --- | --- |
| `dotnet build striv/projects/StriV.Engine.Dominatus/StriV.Engine.Dominatus.csproj -c Debug -v minimal` | 0 | none | Pass | No |
| `dotnet build striv/projects/StriV.Engine.Dominatus.Adapters/StriV.Engine.Dominatus.Adapters.csproj -c Debug -v minimal` | 0 | none | Pass | No |
| `dotnet test striv/tests/StriV.Engine.Dominatus.Tests/StriV.Engine.Dominatus.Tests.csproj -v minimal` | 0 | none | Pass | No |
| `dotnet build striv/projects/Stride.Engine/Stride.Engine.csproj -c Debug -p:StriVWarningFocusProject=Stride.Engine --no-incremental 2>&1 | tee /tmp/striv-m16j-engine-focused.log` | 0 | none | Pass | No |
| `dotnet build striv/StriV.Core.slnx -c Debug -v minimal` | 0 | none | Pass | No |
| `./striv/build/striv-check-focused-projects.sh Stride.BepuPhysics Stride.Core.Mathematics Stride.Core.IO Stride.Input Stride.Games Stride.Core.Reflection` | 0 | none | Pass | No |
| `dotnet test striv/tests/StriV.Engine.Dominatus.Tests/StriV.Engine.Dominatus.Tests.csproj -v minimal` | 0 | none | Pass | No |
| `dotnet test striv/tests/Stride.Core.Reflection.Tests/Stride.Core.Reflection.Tests.csproj -v minimal` | 0 | none | Pass | No |
| `dotnet test striv/tests/Stride.Games.Tests/Stride.Games.Tests.csproj -v minimal` | 0 | none | Pass | No |
| `dotnet test striv/tests/Stride.Input.Tests/Stride.Input.Tests.csproj -v minimal` | 0 | none | Pass | No |
| `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj -v minimal` | 0 | none | Pass | No |
| `dotnet test striv/tests/StriV.AssetTool.Tests/StriV.AssetTool.Tests.csproj -v minimal` | 0 | none | Pass | No |
| `dotnet test striv/tests/StriV.AssetPipeline.Tests/StriV.AssetPipeline.Tests.csproj --no-build -v minimal` | 0 | none | Pass | No |
| `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj --no-build -v minimal` | 0 | none | Pass | No |
| `./striv/build/striv-build-core.sh` | 0 | none | Pass | No |

## 11) Recommended next task

**M16k: implement selected seam (Option C) with focused engine tests and Dominatus adapter integration tests.**

Rationale:

- M16j isolated the exact owner and ordering constraints.
- Minimal seam candidate is now clear and bounded.
- Implementation can proceed without architectural guesswork.
