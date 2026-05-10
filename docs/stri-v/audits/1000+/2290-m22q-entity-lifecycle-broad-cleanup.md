# M22q — EntityLifecycle broad cleanup (test-first) report

## 1) Files changed
- `striv/tests/Stride.Engine.Tests/EntityLifecycleTests.cs`
- `docs/stri-v/audits/1000+/2290-m22q-entity-lifecycle-broad-cleanup.md`

## 2) Task scope
Folder-local EntityLifecycle nullability pass was attempted with test-first lifecycle coverage.
No EntityManager rewrite, no processor matching rewrite, and no Dominatus migration was performed.

## 3) Before warnings
- Focused warning count before: **540**
- EntityLifecycle warning lines before: **182**
- Top EntityLifecycle buckets (before):
  - `EntityManager.cs CS8618` (12)
  - `EntityProcessorCollection.cs CS8765` (4)
  - `Entity.cs CS8618` (10)
  - `EntityComponentCollection.cs CS8625` (12)
  - Several processor-lifecycle buckets under `Engine/EntityLifecycle/Processors/*`

## 4) EntityLifecycle classification table
| File/site | Warning | Pattern | Intended behavior | Category | Action |
| --- | --- | --- | --- | --- | --- |
| Entity.cs | CS8618/CS8625 | attach/detach nullable ownership | entity can exist unattached; detach clears state | detach / ownership clear | covered by tests; source change deferred |
| EntityComponent.cs | CS8618 | component starts unattached | component may have no owner before add | constructor/default state | covered by tests; source change deferred |
| EntityComponentCollection.cs | CS8625/CS8603 | add/remove entity link updates | adding sets owner, removing clears owner | component collection slot semantics | covered by tests; source change deferred |
| TransformComponent parent/children | CS8625 family | parent link optional and removable | detach clears both sides | transform parent/child nullable linkage | covered by tests; source change deferred |
| EntityManager add/remove | CS8618/CS8604 | manager membership set on add/remove | link present while attached only | optional entity manager membership | covered by tests; source change deferred |
| EntityLifecycle processors | CS8602/CS8622/CS8618 | system-injected runtime services and matching contracts | binding occurs during manager registration | processor matching lifecycle | deferred; requires deeper processor-policy audit |

## 5) Tests
Added `EntityLifecycleTests` with focused lifecycle assertions:
- `Entity_DefaultConstruction_HasValidInertState`
- `EntityComponent_DefaultConstruction_IsUnattached`
- `EntityComponentCollection_AddRemove_UpdatesComponentEntityLink`
- `TransformComponent_ParentDetach_ClearsParentAndChildLink`
- `EntityManager_AddRemoveEntity_UpdatesEntityManagerLink`

These tests pin intended lifecycle behavior, not accidental null-reference behavior.

## 6) Fixes applied
Initial nullable-signature/source edits were trialed in EntityLifecycle core files, but they increased focused warnings (540 -> 620) by propagating nullability contract ripple into processor and extension callsites.

To keep convergence and avoid risky behavior-policy rewrites, those source edits were reverted. This pass keeps only the new behavior tests.

## 7) Deferred EntityLifecycle issues
Deferred due to hidden lifecycle invariants and broader policy surface:
- Processor matching and required-type dependency contracts (`EntityProcessor*`, processor implementations)
- Transform propagation/order invariants in transform + processor interactions
- EntityManager policy seams where null encodes lifecycle state transitions
- Dominatus-facing lifecycle state modeling not yet represented in current public contracts

## 8) After warnings
- Focused warning count after: **540**
- EntityLifecycle warning lines after: **182**
- EntityLifecycle delta: **0**
- Total delta: **0**

## 9) Next recommendation
Next practical target should be a **narrowed EntityLifecycle processor-policy sub-pass** (e.g., one processor family at a time with explicit manager-binding tests), rather than broad signature nullability changes.

## 10) Validation results
- `dotnet build striv/projects/Stride.Engine/Stride.Engine.csproj -c Debug -p:StriVWarningFocusProject=Stride.Engine --no-incremental` — exit 0 — pass.
- `dotnet test striv/tests/Stride.Engine.Tests/Stride.Engine.Tests.csproj -v minimal` — exit 0 — pass.

(Outputs truncated in terminal capture: yes.)
