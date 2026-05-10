# M23d — EntityManager dependent-processor contract slice (Dominatus-shaped, Dominatus-free)

## 1) Files changed
- `striv/projects/Stride.Engine/Engine/EntityLifecycle/EntityManager.cs`
- `striv/tests/Stride.Engine.Tests/EntityManagerProcessorPolicyTests.cs`

## 2) Task scope
This slice targeted EntityManager’s dependent-processor and required-type lifecycle seams without rewriting matching behavior. It remains Dominatus-free and only tightens typed internal contracts around `EntityComponentChange` and dependency collection lifecycle.

## 3) Before warnings
- Focused warning count before: **306** (`/tmp/striv-m23d-engine-warning-lines-before.log`).
- Relevant policy warnings included:
  - `EntityManager.cs`: CS8618/CS8625/CS8600/CS8604/CS8618(Dependencies)
  - `EntityProcessor.cs`: CS8602
  - `EntityProcessorCollection.cs`: CS8603

## 4) Contract classification table
| File/site | Warning | Current pattern | Actual lifecycle meaning | Category | Action |
|---|---|---|---|---|---|
| EntityManager.NotifyComponentChanged | nullable old/new pair use | null-checked branching | Added/Removed/Replaced transitions | component-change payload already modeled | Routed through `EntityComponentChange.Kind` switch |
| EntityManager.CheckEntityComponentWithProcessors dependent arg | nullable list parameter | null means “don’t collect” | optional dependency accumulation | dependent processor update | made nullable explicit (`List<EntityProcessor>?`) |
| EntityProcessorCollectionPerComponentType.Dependencies | lazy null list | nullable field + null guards | per-component dependent-processor set | required-type dependency lifecycle | initialized to empty list |
| EntityManager dependent update loop | skip old/new nullable compare | avoid double-processing changed components | revalidate required-type dependent processors | dependent processor update | preserved behavior; now fed from typed change path |
| EntityProcessor.IsDependentOnComponentType | nullable dictionary warning | existing nullable seam | cached required-type acceptance | internal nullable seam needed | deferred |

## 5) Chosen implementation slice
Chosen slice: **A + C (small safe blend)**
- Route `NotifyComponentChanged` through typed `EntityComponentChange.Kind` (A-lite).
- Normalize dependency list lifecycle by initializing `Dependencies` eagerly (C).

Reason: smallest safe change that reduces nullable folklore on dependent processor path without membership algorithm rewrite.

## 6) Contract design
No new public payload was introduced in M23d. Instead, existing `EntityComponentChange` became the authoritative input contract for processor-policy routing in `NotifyComponentChanged`, with explicit Added/Removed/Replaced handling.

Future adapter note: this switch-based internal routing remains compatible with future Dominatus mailbox/actuator adaptation by mapping each branch into processor-membership actuation payloads later.

## 7) Tests
Added/updated required-type processor policy tests:
- `EntityManager_RequiredTypeProcessor_DoesNotMatchUntilAllRequiredComponentsPresent`
- `EntityManager_RequiredTypeProcessor_AddsMembershipWhenRequiredComponentAppears`
- `EntityManager_RequiredTypeProcessor_RemovesMembershipWhenRequiredComponentDisappears`
- `EntityManager_RequiredTypeProcessor_RevalidatesOnRequiredComponentRemoveAdd`

These pin required-type membership and revalidation behavior without private implementation assertions.

## 8) Implementation details
- `NotifyComponentChanged` now switches on `EntityComponentChangeKind` and uses deterministic accessors (`AddedComponent`, `RemovedComponent`) where required.
- Dependent processor collection parameter was made explicitly nullable and named for intent (`currentDependentProcessors`).
- `EntityProcessorCollectionPerComponentType.Dependencies` now always exists as an empty list, removing repeated lazy-null branches.
- Matching/order and processor add/remove/revalidation semantics preserved.

Untouched by design:
- processor matching algorithm,
- associated-data policy internals,
- broad processor-membership payload bus design.

## 9) Dominatus dependency boundary
Command:
`rg -n "Dominatus|Ai\.Act|Ai\.Await|IActuationHandler|StriV.Engine.Dominatus|Dominatus.Core|Dominatus.OptFlow" striv/projects/Stride.Engine -g '*.cs' -g '*.csproj' || true`

Result: **no matches**.

## 10) Warning results
- Focused warning count after: **302**.
- Delta: **-4**.
- Notable bucket shift: `Engine/EntityLifecycle/EntityManager.cs CS8618` reduced (Dependencies init removed one warning pair from duplicated lines in focused log).
- No warning cascade observed in focused project build.

## 11) Deferred policy issues
- Remaining required-type lifecycle seams in `EntityProcessor` caching/nullability.
- Associated-data lifecycle policy unification.
- Processor collection mutation-order risk areas.
- Future Dominatus adapter-stage membership actuation payload mapping.

## 12) Validation results
- `dotnet test striv/tests/Stride.Engine.Tests/Stride.Engine.Tests.csproj -v minimal`
  - exit: 0
  - first meaningful warning/error: none (tests passed)
  - pass/fail: pass
  - output truncated: no
- `dotnet build striv/projects/Stride.Engine/Stride.Engine.csproj -c Debug -p:StriVWarningFocusProject=Stride.Engine --no-incremental`
  - exit: 0
  - first meaningful warning/error: existing CS8765 in `SceneCameraSlotId`
  - pass/fail: pass
  - output truncated: no (log file captured)
- `dotnet build striv/StriV.Core.slnx -c Debug -v minimal`
  - exit: 0
  - first meaningful warning/error: existing CS1030 in `ObjectIdBuilder`
  - pass/fail: pass
  - output truncated: console view truncated by runner; build completed with exit 0

## 13) Next recommendation
**Next EntityManager policy slice**:
Introduce a minimal internal processor-membership typed payload (add/remove/revalidate) only inside EntityManager/processor-policy codepath, with tests asserting deterministic ordering, while continuing to avoid algorithm rewrite.
