# M22r — EntityLifecycle backing-field contract cleanup

## 1) Files changed
- striv/projects/Stride.Engine/Engine/EntityLifecycle/EntityComponent.cs
- striv/projects/Stride.Engine/Engine/EntityLifecycle/Entity.cs
- striv/projects/Stride.Engine/Engine/EntityLifecycle/EntityProcessor.cs
- striv/projects/Stride.Engine/Engine/EntityLifecycle/EntityComponentCollection.cs
- striv/projects/Stride.Engine/Engine/EntityLifecycle/EntityManager.cs
- striv/projects/Stride.Engine/Engine/CloneLifecycle/CloneSerializer.cs
- striv/tests/Stride.Engine.Tests/EntityLifecycleTests.cs
- docs/stri-v/audits/1000+/2300-m22r-entity-lifecycle-backing-field-contract-cleanup.md

## 2) Task scope
Narrowed lifecycle-contract pass only. No EntityManager rewrite, no processor matching rewrite, no Dominatus migration.

## 3) Before warnings
- Focused count before: 540
- EntityLifecycle warning lines before: 182

## 4) Claude audit hypothesis
Public nullable lifecycle-membership properties caused nullable propagation. Correct pattern is private nullable state + guarded non-null public accessors; keep genuinely optional relationships nullable (`Scene`, `SceneValue`, `Parent`).

## 5) Classification table
| Type | Member | Semantic category | Public contract | Backing storage | Action |
| ---- | ------ | ----------------- | --------------- | --------------- | ------ |
| EntityComponent | Entity | lifecycle-initialized membership | non-null guarded | `Entity? entity` | applied |
| Entity | EntityManager | lifecycle-initialized membership | non-null guarded | `EntityManager? entityManager` | applied |
| EntityProcessor | EntityManager | lifecycle-initialized membership | non-null guarded | `EntityManager? entityManager` | applied |
| EntityProcessor | Services | lifecycle-initialized membership | non-null guarded | `IServiceRegistry? services` | applied |
| TransformComponent | Parent | optional relationship | nullable | existing nullable behavior | unchanged |
| Entity | Scene / SceneValue | optional relationship | nullable | existing nullable behavior | unchanged |

## 6) Tests
Added tests asserting deterministic lifecycle misuse failures and attached success paths for component/entity/processor access.

## 7) Fixes applied
- Converted membership properties to guarded non-null public accessors with internal nullable setters.
- Updated attach/detach call sites to use `SetEntity`, `SetEntityManager`, `SetServices`.
- Updated clone serializer to use nullable component backing access (`EntityOrNull`).

## 8) Warning results
- Focused warning count after: 522
- EntityLifecycle warning lines: 182 -> 164
- Cascade avoided (count did not increase; reduced).

## 9) Deferred issues
- processor matching invariants
- transform propagation/order invariants
- EntityManager policy seams
- Dominatus lifecycle modeling

## 10) Validation results
- `dotnet build striv/projects/Stride.Engine/Stride.Engine.csproj -c Debug -p:StriVWarningFocusProject=Stride.Engine --no-incremental` => exit 0, pass, truncated yes
- `dotnet test striv/tests/Stride.Engine.Tests/Stride.Engine.Tests.csproj -v minimal` => exit 1, failed (existing broader suite issues), truncated yes

## 11) Next recommendation
Continue narrowed EntityLifecycle processor-policy pass while keeping guarded membership contracts.
