# 2340 — M22w Stride.Engine finishing sweep wave 2

## 1) Files changed
- striv/projects/Stride.Engine/Engine/CloneLifecycle/CloneEntityComponentData.cs
- striv/projects/Stride.Engine/Engine/RenderingLifecycle/InstanceComponent.cs
- striv/projects/Stride.Engine/Engine/RenderingLifecycle/InstancingUserBuffer.cs
- striv/projects/Stride.Engine/Engine/RenderingLifecycle/Skyboxes/CubemapRendererBase.cs
- striv/projects/Stride.Engine/Engine/RenderingLifecycle/Skyboxes/CubemapSceneRenderer.cs
- docs/stri-v/audits/1000+/2340-m22w-stride-engine-finishing-sweep-2.md

## 2) Task scope
Wave 2 finishing sweep focused on low-risk nullability/default-state fixes in localized Stride.Engine files. STRIDE2000 and UpdateEngine runtime invariants were deferred, with no architecture rewrites, no warning suppressions, and no Dominatus migration.

## 3) Before warnings
- Focused warning count before: **438**
- Top buckets before included UpdateEngine CS8600/CS8604, EntityManager CS8618/CS8604, InputSystem CS8622, Clone/Diagnostics buckets, and STRIDE2000 in design resolvers.

## 4) Classification table
| Bucket | Warning | File(s) | Category | Action |
|---|---|---|---|---|
| Clone data defaults | CS8618/CS8604 | CloneEntityComponentData.cs | constructor/default state | Added inert defaults and nullable-aware merge handling |
| Rendering instance defaults | CS8618/CS8622 | InstanceComponent.cs, InstancingUserBuffer.cs | rendering component optional state | Made backing fields/event sender nullable-safe |
| Cubemap lifecycle defaults | CS8618 | CubemapRendererBase.cs, CubemapSceneRenderer.cs | rendering component optional state | Marked optional fields nullable and retained lifecycle behavior |
| UpdateEngine | CS8600/CS8604 | UpdateEngine.cs | UpdateEngine defer | Deferred |
| STRIDE2000 | STRIDE2000 | ParameterCollectionResolver, EntityChildPropertyResolver | STRIDE2000 defer | Deferred |
| Network async buckets | CS4014 | Quarantine/Network | policy-heavy defer | Deferred |

## 5) Tests
- No new tests were added in this pass; behavior-affecting semantics were not changed (default-state/type-nullability only).
- Existing verification used `Stride.Engine.Tests` pass to validate no regression in current lifecycle coverage.

## 6) Fixes applied
- `CloneEntityComponentData`: converted `Entity` to nullable and initialized `Properties`; updated `MergeObject` to accept nullable values and guard list/dictionary merge shape checks.
- `InstanceComponent`: nullable backing fields for disconnected state, nullable sender event signature, and guarded getter contract for unset `Master`.
- `InstancingUserBuffer`: marked runtime-populated GPU buffers nullable.
- `CubemapRendererBase`: nullable optional depth stencil and explicit default-init for `DrawContext`.
- `CubemapSceneRenderer`: compositor snapshot stored as nullable to reflect attach/detach lifecycle.

## 7) Deferred issues
- STRIDE2000 buckets.
- UpdateEngine runtime navigation invariants.
- EntityManager processor matching/required-type policy areas.
- Deep GPU/render pipeline invariants.
- Quarantine/Network async fire-and-forget warnings.
- Camera slot lifecycle policy-heavy paths.

## 8) Warning results
- Focused warning count after: **414**
- Delta: **-24**
- Cleared/reduced buckets include `InstancingUserBuffer CS8618`, `InstanceComponent CS8622/CS8618`, and portions of clone/component default-state warnings.
- Remaining top buckets still led by UpdateEngine/EntityManager/InputSystem/Diagnostics/STRIDE2000.

## 9) Validation results
- `dotnet build striv/projects/Stride.Engine/Stride.Engine.csproj -c Debug -p:StriVWarningFocusProject=Stride.Engine --no-incremental` → exit 0, pass, output truncated: yes.
- `dotnet test striv/tests/Stride.Engine.Tests/Stride.Engine.Tests.csproj -v minimal` → exit 0, pass, output truncated: no.
- `dotnet build striv/StriV.Core.slnx -c Debug -v minimal` → exit 0, pass, first meaningful warnings are existing analyzer/nullability warnings outside M22w scope, output truncated: yes.

## 10) Next recommendation
Proceed with **finishing sweep 3** focused on remaining safe InputSystem/diagnostics/entity default-state buckets, while continuing to defer STRIDE2000 and UpdateEngine policy/runtime invariants.
