# 2370 — M23a Stride.Engine post-STRIDE2000 finishing sweep

## 1) Files changed
- striv/projects/Stride.Engine/Engine/AnimationLifecycle/CompressedTimeSpan.cs
- striv/projects/Stride.Engine/Engine/AnimationLifecycle/ComputeAnimationCurve.cs
- striv/projects/Stride.Engine/Engine/EntityLifecycle/EntityProcessorCollection.cs
- striv/projects/Stride.Engine/Engine/RenderingLifecycle/AnimationComponent.cs
- docs/stri-v/audits/1000+/2370-m23a-stride-engine-post-stride2000-finishing-sweep.md

## 2) Task scope
M23a re-baselined focused warnings after manual STRIDE2000 handling, then applied a narrow safe sweep for local C# nullability signature mismatches only. No subsystem rewrite, no Dominatus migration, no warning suppression.

## 3) STRIDE2000 status
- Before: no STRIDE2000 found in focused warning lines.
- After: no STRIDE2000 found in focused warning lines.
- Manual STRIDE2000 cleanup appears preserved.

## 4) Before warnings
- Focused warning line count before: 336
- Top warning IDs before: CS8618 (66), CS8625 (60), CS8602 (54), CS8601 (40), CS8603 (34), CS8604 (32), CS8600 (26)
- Top file buckets before: UpdateEngine.cs CS8600 (12), EntityManager.cs CS8618 (12), EntityManager.cs CS8604 (10)

## 5) Classification table
| Bucket | Warning | File(s) | Category | Action |
| --- | --- | --- | --- | --- |
| comparer/object signature nullability | CS8765/CS8767 | CompressedTimeSpan.cs, ComputeAnimationCurve.cs, EntityProcessorCollection.cs, AnimationComponent.cs | local nullable return/argument contract, event/delegate nullability | fixed now |
| UpdateEngine null-state flow | CS8600/CS8601/CS8604 | UpdateEngine.cs | UpdateEngine runtime invariant | deferred |
| EntityManager processor policy | CS8618/CS8604/CS8625/CS8600 | EntityManager.cs | EntityManager processor policy | deferred |
| render lifecycle nullability | CS8602/CS8625/CS8618 | ForwardRenderer.cs, ModelRenderProcessor.cs, LightShaft*.cs | render lifecycle invariant | deferred |

## 6) Tests
No behavior-changing logic was introduced; no new tests added. Existing test suites were executed for regression coverage.

## 7) Fixes applied
- CompressedTimeSpan: aligned overrides/interfaces to nullable object parameters (`Equals(object?)`, `CompareTo(object?)`).
- ComputeAnimationCurve: aligned comparer override to nullable keyframe parameters.
- EntityProcessorCollection: aligned comparer override to nullable params and added deterministic null-order guards.
- AnimationComponent: aligned event handler sender signature to `object?`.

## 8) Deferred issues
- UpdateEngine runtime invariants.
- EntityManager processor matching/required-type policy.
- Render/GPU lifecycle invariants.
- Associated-data policy edges in processor pipelines.

## 9) Warning results
- Focused warning line count after: 322
- Delta: -14 focused warning lines.
- STRIDE2000: 0 before, 0 after.
- Remaining top buckets continue to cluster in policy/runtime-invariant areas (UpdateEngine, EntityManager, rendering lifecycle).

## 10) Validation results
Primary executed commands completed with exit code 0 unless noted in shell logs.
- `dotnet build striv/projects/Stride.Engine/Stride.Engine.csproj -c Debug -p:StriVWarningFocusProject=Stride.Engine --no-incremental`
- `dotnet test striv/tests/Stride.Engine.Tests/Stride.Engine.Tests.csproj -v minimal`
- `dotnet build striv/StriV.Core.slnx -c Debug -v minimal`
- `./striv/build/striv-check-focused-projects.sh Stride.BepuPhysics Stride.Core.Mathematics Stride.Core.IO Stride.Input Stride.Games Stride.Core.Reflection`
- `dotnet test striv/tests/StriV.Engine.Dominatus.Tests/StriV.Engine.Dominatus.Tests.csproj -v minimal`
- `dotnet test striv/tests/Stride.Core.Reflection.Tests/Stride.Core.Reflection.Tests.csproj -v minimal`
- `dotnet test striv/tests/Stride.Games.Tests/Stride.Games.Tests.csproj -v minimal`
- `dotnet test striv/tests/Stride.Input.Tests/Stride.Input.Tests.csproj -v minimal`

## 11) Next recommendation
Recommend **M23b finishing sweep** focused on additional safe, local signature and constructor-default buckets outside deferred runtime/policy invariant hotspots.
