# M23b — EntityManager processor-policy contract slice

## 1) Files changed
- `striv/projects/Stride.Engine/Engine/EntityLifecycle/EntityProcessor.cs`
- `striv/tests/Stride.Engine.Tests/EntityManagerProcessorPolicyTests.cs`
- `docs/stri-v/audits/1000+/2380-m23b-entitymanager-processor-policy-contract-slice.md`

## 2) Task scope
Implemented first safe, test-first slice for EntityManager/processor lifecycle nullability contracts without changing processor matching behavior, required-type dependency behavior, or Dominatus lifecycle policy.

## 3) Before warnings
- Focused warning lines before: `322`.
- Relevant warning sites before included:
  - `EntityProcessor.cs` CS8604 at associated-data remove/revalidate callsites.
  - `EntityManager.cs` CS8618/CS8604/CS8625/CS8600 policy-heavy sites.

## 4) Warning classification table
| File/site | Warning | Pattern | Intended behavior | Category | Action |
| --- | --- | --- | --- | --- | --- |
| EntityProcessor.cs:300/307 | CS8604 | nullable `entityData` passed to remove/validate callbacks | associated data must exist when component is currently tracked | associated-data lifecycle | **fixed in this slice** via owner-scoped retrieval from `ComponentDatas` under tracked branch |
| EntityProcessor.cs:53/203 | CS8618/CS8602 | required-type support map only created for processors with required types | map absent is valid when no required types | required-type dependency lifecycle | made field nullable, left matching semantics unchanged |
| EntityManager.cs:82 events | CS8618 | non-nullable events not initialized | classic event-nullability pattern | constructor/default state | defer (separate event contract pass) |
| EntityManager.cs:541/546 | CS8604 | possible null old/new component passed intentionally | add/remove pipeline accepts absent side | processor matching policy | defer (signature/contract design needed) |
| EntityManager.cs:701 | CS8618 | dependency list lazily initialized | null means no dependencies | required-type dependency lifecycle | defer (internal container contract refactor needed) |

## 5) Chosen implementation slice
Selected **Slice B (Associated-data remove guard semantics)** with a small local contract tightening in generic processor lifecycle:
- When tracked (`entityAdded == true`), associated data is retrieved from owner map (`ComponentDatas[entityComponent]`) and treated as required.
- No matching algorithm rewrite, no required-type behavior rewrite.

## 6) Tests
Added `EntityManagerProcessorPolicyTests`:
- `EntityProcessor_AssociatedData_AddRemoveLifecycle_PassesNonNullDataWhenMatched`
- `EntityProcessor_AssociatedData_RemoveWithoutPriorAdd_DoesNotNullReference`

These pin deterministic behavior for add/remove lifecycle and prevent accidental NRE behavior in remove-without-match path.

## 7) Fixes applied
- Reworked tracked-entity remove/revalidation branches in `EntityProcessor<TComponent,TData>.ProcessEntityComponent` to fetch associated data from `ComponentDatas` only in branches where membership exists.
- Tightened branch conditions to `entityMatch && entityAdded` for revalidation path.
- Marked `componentTypesSupportedAsRequired` as nullable because null is a valid state when `RequiredTypes.Length == 0`.

## 8) Deferred policy issues
Deferred intentionally (policy-heavy):
- `EntityManager` component-change null-flow signatures and dependent-processor update policy.
- required-type dependency map lifecycle harmonization in `EntityManager` collections.
- processor collection mutation order/algorithm review.
- Dominatus-forward lifecycle modeling.

## 9) Warning results
- Focused warning lines after: `318`.
- Delta: `-4`.
- Cascade avoided (no broad public nullable expansion, no suppressions).
- EntityManager policy-heavy buckets remain and are explicitly deferred.

## 10) Validation results
Executed command suite from prompt; all commands exited `0`.
- Focused before build/logging: pass.
- Engine tests: pass.
- Focused after build/logging: pass.
- `dotnet build striv/StriV.Core.slnx -c Debug -v minimal`: pass.
- `striv-check-focused-projects.sh` set: pass.
- Required project test suite commands: pass.
- `./striv/build/striv-build-core.sh`: pass.

Output was truncated in terminal capture for very long warning/build logs, but commands completed successfully with exit code 0.

## 11) Next recommendation
Next best slice: **EntityManager component-change null-flow contract pass** (method signatures + local helper contracts around old/new component absent-state), because it is now the dominant remaining EntityLifecycle policy warning cluster.
